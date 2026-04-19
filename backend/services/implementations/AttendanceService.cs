using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class AttendanceService(AppDbContext db) : IAttendanceService
{
    public async Task MarkStudentAttendanceOnLoginAsync(Guid userId, DateTimeOffset nowUtc)
    {
        var studentId = await db.Students
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (studentId is null)
        {
            return;
        }

        var settings = await GetSettingsEntityAsync();
        var tz = GetTimeZone(settings.TimeZoneId);
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, tz);
        var localTime = TimeOnly.FromDateTime(nowLocal.DateTime);
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var dow = nowLocal.DayOfWeek;

        if (localTime < settings.CheckInStartLocal || localTime > settings.CheckInEndLocal)
        {
            return;
        }

        var eligibleModules = await (
            from sme in db.StudentModuleEnrollments
            join m in db.Modules on sme.ModuleId equals m.Id
            where sme.StudentId == studentId.Value
                  && !sme.IsDeleted
                  && !m.IsDeleted
                  && m.ScheduledDay == dow
                  && localTime >= m.ScheduledStartLocal
                  && localTime <= m.ScheduledEndLocal
                  && today >= m.RunsFrom
                  && today <= m.RunsTo
            select new { m.Id, m.ModuleCode }
        ).ToListAsync();

        if (eligibleModules.Count == 0)
        {
            return;
        }

        var moduleIds = eligibleModules.Select(x => x.Id).ToList();

        var existing = await db.StudentAttendances
            .Where(a => a.StudentId == studentId.Value && a.Date == today && moduleIds.Contains(a.ModuleId))
            .Select(a => a.ModuleId)
            .ToListAsync();

        var existingSet = existing.ToHashSet();

        foreach (var mod in eligibleModules)
        {
            if (existingSet.Contains(mod.Id))
            {
                continue;
            }

            db.StudentAttendances.Add(new StudentAttendance
            {
                StudentId = studentId.Value,
                ModuleId = mod.Id,
                Date = today,
                CheckedInAtUtc = nowUtc
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task<MyAttendanceResponseDto> GetMyAttendanceAsync(Guid meUserId, DateOnly from, DateOnly to, int page,
        int pageSize)
    {
        var studentId = await db.Students
            .Where(s => s.UserId == meUserId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (studentId is null)
        {
            throw new AppException(404, "STUDENT_NOT_FOUND", "Student profile not found.");
        }

        return await GetStudentAttendanceCoreAsync(studentId.Value, from, to, page, pageSize);
    }

    private sealed record ExpectedEnrollmentRow(
        Guid StudentId,
        DayOfWeek ScheduledDay,
        DateOnly RunsFrom,
        DateOnly RunsTo
    );

    public async Task<PagedDto<AdminStudentAttendanceRowDto>> GetStudentsAttendanceAsync(DateOnly fromDate, DateOnly to,
        string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var studentsQ =
            from s in db.Students
            join u in db.Users on s.UserId equals u.Id
            where !s.IsDeleted && !u.IsDeleted
            select new
            {
                s.Id,
                s.UserId,
                s.StudentNumber,
                u.FirstName,
                u.LastName,
                u.Email
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            studentsQ = studentsQ.Where(x =>
                x.StudentNumber.ToLower().Contains(q) ||
                x.FirstName.ToLower().Contains(q) ||
                x.LastName.ToLower().Contains(q) ||
                x.Email.ToLower().Contains(q));
        }

        var total = await studentsQ.CountAsync();

        var pageStudents = await studentsQ
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var studentIds = pageStudents.Select(x => x.Id).ToList();

        var attendedByStudent = await db.StudentAttendances
            .Where(a => studentIds.Contains(a.StudentId) && a.Date >= fromDate && a.Date <= to)
            .GroupBy(a => a.StudentId)
            .Select(g => new { StudentId = g.Key, Attended = g.Count() })
            .ToListAsync();

        var attendedDict = attendedByStudent.ToDictionary(x => x.StudentId, x => x.Attended);

        var enrollments = await (
            from sme in db.StudentModuleEnrollments
            join m in db.Modules on sme.ModuleId equals m.Id
            where studentIds.Contains(sme.StudentId) && !sme.IsDeleted && !m.IsDeleted
                  && m.RunsFrom <= to && m.RunsTo >= fromDate
            select new ExpectedEnrollmentRow(
                sme.StudentId,
                m.ScheduledDay,
                m.RunsFrom,
                m.RunsTo
            )
        ).ToListAsync();

        var expectedDict = ComputeExpectedTotalsByStudent(enrollments, fromDate, to);

        var items = pageStudents.Select(s =>
        {
            var expected = expectedDict.GetValueOrDefault(s.Id, 0);
            var attended = attendedDict.GetValueOrDefault(s.Id, 0);
            double? pct = expected == 0 ? null : (double)attended / expected;

            return new AdminStudentAttendanceRowDto(
                s.Id,
                s.UserId,
                s.StudentNumber,
                s.FirstName,
                s.LastName,
                expected,
                attended,
                pct
            );
        }).ToList();

        return new PagedDto<AdminStudentAttendanceRowDto>(items, page, pageSize, total);
    }

    public Task<MyAttendanceResponseDto> GetStudentAttendanceAsync(Guid studentId, DateOnly from, DateOnly to, int page,
        int pageSize) => GetStudentAttendanceCoreAsync(studentId, from, to, page, pageSize);

    public async Task<PagedDto<StaffModuleStudentAttendanceRowDto>> GetModuleAttendanceAsync(Guid moduleId,
        DateOnly from,
        DateOnly to, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var module = await db.Modules
            .Where(m => m.Id == moduleId && !m.IsDeleted)
            .Select(m => new { m.Id, m.ScheduledDay, m.RunsFrom, m.RunsTo, m.ModuleCode, m.Title })
            .FirstOrDefaultAsync();

        if (module is null)
            throw new AppException(404, "MODULE_NOT_FOUND", "Module not found.");

        var effectiveFrom = Max(from, module.RunsFrom);
        var effectiveTo = Min(to, module.RunsTo);
        var expected = CountScheduledOccurrences(effectiveFrom, effectiveTo, module.ScheduledDay);

        var q =
            from sme in db.StudentModuleEnrollments
            join s in db.Students on sme.StudentId equals s.Id
            join u in db.Users on s.UserId equals u.Id
            where sme.ModuleId == moduleId && !sme.IsDeleted && !s.IsDeleted && !u.IsDeleted
            select new
            {
                StudentId = s.Id,
                s.StudentNumber,
                u.FirstName,
                u.LastName,
                u.Email
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.StudentNumber.ToLower().Contains(term) ||
                x.FirstName.ToLower().Contains(term) ||
                x.LastName.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term));
        }

        var total = await q.CountAsync();

        var pageStudents = await q
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var studentIds = pageStudents.Select(x => x.StudentId).ToList();

        var attended = await db.StudentAttendances
            .Where(a => a.ModuleId == moduleId
                        && studentIds.Contains(a.StudentId)
                        && a.Date >= effectiveFrom && a.Date <= effectiveTo)
            .GroupBy(a => a.StudentId)
            .Select(g => new { StudentId = g.Key, Attended = g.Count() })
            .ToListAsync();

        var attendedDict = attended.ToDictionary(x => x.StudentId, x => x.Attended);

        var items = pageStudents.Select(s =>
        {
            var a = attendedDict.GetValueOrDefault(s.StudentId, 0);
            double? pct = expected == 0 ? null : (double)a / expected;

            return new StaffModuleStudentAttendanceRowDto(
                s.StudentId,
                s.StudentNumber,
                s.FirstName,
                s.LastName,
                expected,
                a,
                pct
            );
        }).ToList();

        return new PagedDto<StaffModuleStudentAttendanceRowDto>(items, page, pageSize, total);
    }

    public async Task<AttendanceSettingsDto> GetSettingsAsync()
    {
        var s = await GetSettingsEntityAsync();
        return new AttendanceSettingsDto(s.CheckInStartLocal, s.CheckInEndLocal, s.TimeZoneId);
    }

    public async Task<AttendanceSettingsDto> UpdateSettingsAsync(UpdateAttendanceSettingsDto dto)
    {
        if (dto.CheckInStartLocal >= dto.CheckInEndLocal)
            throw new AppException(400, "INVALID_WINDOW", "Check-in start must be before check-in end.");

        _ = GetTimeZone(dto.TimeZoneId);

        var s = await GetSettingsEntityAsync();
        s.CheckInStartLocal = dto.CheckInStartLocal;
        s.CheckInEndLocal = dto.CheckInEndLocal;
        s.TimeZoneId = dto.TimeZoneId;

        await db.SaveChangesAsync();
        return new AttendanceSettingsDto(s.CheckInStartLocal, s.CheckInEndLocal, s.TimeZoneId);
    }

    private async Task<MyAttendanceResponseDto> GetStudentAttendanceCoreAsync(Guid studentId, DateOnly fromDate,
        DateOnly to, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var modules = await (
            from sme in db.StudentModuleEnrollments
            join m in db.Modules on sme.ModuleId equals m.Id
            where sme.StudentId == studentId && !sme.IsDeleted && !m.IsDeleted
                  && m.RunsFrom <= to && m.RunsTo >= fromDate
            select new
            {
                m.Id,
                m.ModuleCode,
                m.Title,
                m.ScheduledDay,
                m.ScheduledStartLocal,
                m.ScheduledEndLocal,
                m.RunsFrom,
                m.RunsTo
            }
        ).ToListAsync();

        var attInRangeQ = db.StudentAttendances
            .Where(a => a.StudentId == studentId && a.Date >= fromDate && a.Date <= to);

        var attendedTotal = await attInRangeQ.CountAsync();

        var attendedPerModule = await attInRangeQ
            .GroupBy(a => a.ModuleId)
            .Select(g => new { ModuleId = g.Key, Attended = g.Count() })
            .ToListAsync();

        var attendedPerModuleDict = attendedPerModule.ToDictionary(x => x.ModuleId, x => x.Attended);

        var perModule = new List<MyModuleAttendanceSummaryDto>();
        var expectedTotal = 0;

        foreach (var m in modules)
        {
            var effectiveFrom = Max(fromDate, m.RunsFrom);
            var effectiveTo = Min(to, m.RunsTo);
            var expected = CountScheduledOccurrences(effectiveFrom, effectiveTo, m.ScheduledDay);

            if (expected == 0) continue;

            var attended = attendedPerModuleDict.GetValueOrDefault(m.Id, 0);

            expectedTotal += expected;

            perModule.Add(new MyModuleAttendanceSummaryDto(
                m.Id,
                $"{m.ModuleCode} - {m.Title}",
                m.ScheduledDay,
                m.RunsFrom,
                m.RunsTo,
                expected,
                attended,
                (double)attended / expected
            ));
        }

        double? overallPct = expectedTotal == 0 ? null : (double)attendedTotal / expectedTotal;

        var overview = new MyAttendanceOverviewDto(
            fromDate,
            to,
            expectedTotal,
            attendedTotal,
            overallPct,
            perModule.OrderBy(x => x.ModuleName).ToList());

        var allRelevantDates = new HashSet<DateOnly>();
        foreach (var m in modules)
        {
            var effectiveFrom = Max(fromDate, m.RunsFrom);
            var effectiveTo = Min(to, m.RunsTo);

            for (var d = effectiveFrom; d.DayNumber <= effectiveTo.DayNumber; d = d.AddDays(1))
            {
                var dow = d.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                if (dow == m.ScheduledDay)
                {
                    allRelevantDates.Add(d);
                }
            }
        }

        var totalDaysWithSchedule = allRelevantDates.Count;

        var pageDates = allRelevantDates
            .OrderByDescending(d => d)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dayAttendance = await (
            from a in db.StudentAttendances
            join m in db.Modules on a.ModuleId equals m.Id
            where a.StudentId == studentId
                  && pageDates.Contains(a.Date)
                  && !m.IsDeleted
            select new
            {
                a.Date,
                a.ModuleId,
                a.CheckedInAtUtc
            }
        ).ToListAsync();

        var attendanceByDateAndModule = dayAttendance
            .ToDictionary(x => (x.Date, x.ModuleId), x => x.CheckedInAtUtc);

        var days = pageDates
            .OrderByDescending(d => d)
            .Select(d =>
            {
                var dow = d.ToDateTime(TimeOnly.MinValue).DayOfWeek;

                var scheduledModules = modules
                    .Where(m => m.ScheduledDay == dow && d >= m.RunsFrom && d <= m.RunsTo)
                    .OrderBy(m => m.ScheduledStartLocal)
                    .ThenBy(m => m.ModuleCode)
                    .Select(m =>
                    {
                        var key = (d, m.Id);
                        var isAttended = attendanceByDateAndModule.TryGetValue(key, out var checkedInAt);
                        return new MyAttendanceDayModuleDto(
                            m.Id,
                            $"{m.ModuleCode} - {m.Title}",
                            isAttended,
                            isAttended ? checkedInAt : null,
                            m.ScheduledStartLocal,
                            m.ScheduledEndLocal
                        );
                    })
                    .ToList();

                var attendedCount = scheduledModules.Count(m => m.IsAttended);

                return new MyAttendanceDayDto(d, attendedCount, scheduledModules);
            })
            .ToList();

        return new MyAttendanceResponseDto(
            overview,
            new PagedDto<MyAttendanceDayDto>(days, page, pageSize, totalDaysWithSchedule)
        );
    }

    private async Task<AttendanceSettings> GetSettingsEntityAsync()
    {
        var s = await db.AttendanceSettings.OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync();

        if (s is not null)
        {
            return s;
        }

        s = new AttendanceSettings
        {
            CheckInStartLocal = new TimeOnly(8, 30),
            CheckInEndLocal = new TimeOnly(10, 0),
            TimeZoneId = "Europe/London"
        };

        db.AttendanceSettings.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    private static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            throw new AppException(400, "INVALID_TIMEZONE", $"Invalid timezone Id: '{timeZoneId}'");
        }
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a.DayNumber >= b.DayNumber ? a : b;
    private static DateOnly Min(DateOnly a, DateOnly b) => a.DayNumber <= b.DayNumber ? a : b;

    private static int CountScheduledOccurrences(DateOnly from, DateOnly to, DayOfWeek scheduledDay)
    {
        if (to < from)
        {
            return 0;
        }

        var cnt = 0;

        for (var d = from; d.DayNumber <= to.DayNumber; d = d.AddDays(1))
        {
            var dow = d.ToDateTime(TimeOnly.MinValue).DayOfWeek;
            if (dow == scheduledDay)
            {
                cnt++;
            }
        }

        return cnt;
    }

    private static Dictionary<Guid, int> ComputeExpectedTotalsByStudent(List<ExpectedEnrollmentRow> enrollments,
        DateOnly from,
        DateOnly to)
    {
        var dict = new Dictionary<Guid, int>();

        foreach (var (studentId, scheduledDay, runsFrom, runsTo) in enrollments)
        {
            var effFrom = Max(from, runsFrom);
            var effTo = Min(to, runsTo);
            var expected = CountScheduledOccurrences(effFrom, effTo, scheduledDay);

            dict[studentId] = dict.TryGetValue(studentId, out var current) ? current + expected : expected;
        }

        return dict;
    }
}
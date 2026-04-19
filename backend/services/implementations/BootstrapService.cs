using System.Text.Json;
using backend.auth;
using backend.data;
using backend.errors;
using backend.models;
using backend.models.enums;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class BootstrapService(AppDbContext db) : IBootstrapService
{
    public async Task Boostrap()
    {
        await SeedSystemRolesAsync();

        var adminRole = await db.Roles.FirstAsync(r => r.Key == "admin");
        var adminExists = await db.UserRoles.AnyAsync(r => r.RoleId == adminRole.Id && !r.IsDeleted);

        if (adminExists)
        {
            throw new AppException(409, "ADMIN_ALREADY_EXISTS", "An admin user already exists");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword("Test123!?");

        var user = new User
        {
            Email = "admin@uni.com",
            FirstName = "admin",
            LastName = "admin",
            DateOfBirth = DateOnly.MinValue,
            PasswordHash = hash,
            IsActive = true,
            Permissions = adminRole.Permissions
        };

        db.Users.Add(user);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = adminRole.Id
        };

        db.UserRoles.Add(userRole);

        var staff = new Staff
        {
            UserId = user.Id,
            StaffNumber = "a000001",
            Department = "unassigned"
        };

        db.Staff.Add(staff);

        await db.SaveChangesAsync();
    }

    public async Task Propagate()
    {
        await SeedSystemRolesAsync();

        var staffRole = await db.Roles.FirstAsync(r => r.Key == "staff");
        var studentRole = await db.Roles.FirstAsync(r => r.Key == "student");

        var staffMembers = new[]
        {
            MakeStaff("james.thornton", "James", "Thornton", new DateOnly(1975, 3, 14), "s000001", "Computer Science"),
            MakeStaff("priya.sharma", "Priya", "Sharma", new DateOnly(1980, 7, 22), "s000002", "Computer Science"),
            MakeStaff("oliver.nguyen", "Oliver", "Nguyen", new DateOnly(1978, 11, 5), "s000003", "Mathematics"),
            MakeStaff("sarah.oconnell", "Sarah", "O'Connell", new DateOnly(1983, 2, 28), "s000004", "Engineering"),
            MakeStaff("michael.davies", "Michael", "Davies", new DateOnly(1970, 9, 17), "s000005", "Computer Science"),
        };

        foreach (var (user, _) in staffMembers)
        {
            db.Users.Add(user);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = staffRole.Id });
        }

        await db.SaveChangesAsync();

        foreach (var (_, staff) in staffMembers)
        {
            db.Staff.Add(staff);
        }

        await db.SaveChangesAsync();

        var csBsc = new Course
        {
            CourseCode = "CS-BSC",
            Title = "BSc Computer Science",
            Description =
                "A three-year undergraduate course covering algorithms, software engineering, AI and systems.",
            Award = "BSc (Hons)",
            DurationSemesters = 6,
            IsActive = true
        };

        var seBsc = new Course
        {
            CourseCode = "SE-BSC",
            Title = "BSc Software Engineering",
            Description = "A three-year undergraduate course focused on software design, testing and agile delivery.",
            Award = "BSc (Hons)",
            DurationSemesters = 6,
            IsActive = true
        };

        var msMsc = new Course
        {
            CourseCode = "MS-MSC",
            Title = "MSc Data Science",
            Description =
                "A one-year postgraduate course covering machine learning, big data and statistical modelling.",
            Award = "MSc",
            DurationSemesters = 2,
            IsActive = true
        };

        db.Courses.AddRange(csBsc, seBsc, msMsc);
        await db.SaveChangesAsync();

        var attendanceSettings = new AttendanceSettings
        {
            CheckInStartLocal = new TimeOnly(8, 45),
            CheckInEndLocal = new TimeOnly(9, 15),
            TimeZoneId = "Europe/London",
            IsActive = true
        };

        db.AttendanceSettings.Add(attendanceSettings);
        await db.SaveChangesAsync();

        var sem1From = new DateOnly(2025, 9, 22);
        var sem1To = new DateOnly(2026, 1, 30);
        var sem2From = new DateOnly(2026, 2, 2);
        var sem2To = new DateOnly(2026, 6, 19);

        var jt = staffMembers[0].Item2; // James Thornton
        var ps = staffMembers[1].Item2; // Priya Sharma
        var on = staffMembers[2].Item2; // Oliver Nguyen
        var so = staffMembers[3].Item2; // Sarah O'Connell
        var md = staffMembers[4].Item2; // Michael Davies

        var csAlgo = MakeModule(csBsc, "CS1001", "Algorithms & Data Structures", DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(11, 0), sem1From, sem1To, year: 1, sem: 1, credits: 20, level: 4);
        var csProg = MakeModule(csBsc, "CS1002", "Programming Fundamentals", DayOfWeek.Wednesday,
            new TimeOnly(13, 0), new TimeOnly(15, 0), sem1From, sem1To, year: 1, sem: 1, credits: 20, level: 4);
        var csMath = MakeModule(csBsc, "CS1003", "Discrete Mathematics", DayOfWeek.Thursday,
            new TimeOnly(11, 0), new TimeOnly(13, 0), sem1From, sem1To, year: 1, sem: 1, credits: 20, level: 4);

        var csOs = MakeModule(csBsc, "CS1004", "Operating Systems", DayOfWeek.Tuesday,
            new TimeOnly(10, 0), new TimeOnly(12, 0), sem2From, sem2To, year: 1, sem: 2, credits: 20, level: 4);
        var csDb = MakeModule(csBsc, "CS1005", "Database Systems", DayOfWeek.Friday,
            new TimeOnly(9, 0), new TimeOnly(11, 0), sem2From, sem2To, year: 1, sem: 2, credits: 20, level: 4);

        var csAi = MakeModule(csBsc, "CS2001", "Artificial Intelligence", DayOfWeek.Monday,
            new TimeOnly(14, 0), new TimeOnly(16, 0), sem1From, sem1To, year: 2, sem: 1, credits: 20, level: 5);
        var csNet = MakeModule(csBsc, "CS2002", "Computer Networks", DayOfWeek.Wednesday,
            new TimeOnly(9, 0), new TimeOnly(11, 0), sem1From, sem1To, year: 2, sem: 1, credits: 20, level: 5);

        var csSec = MakeModule(csBsc, "CS2003", "Cybersecurity Fundamentals", DayOfWeek.Thursday,
            new TimeOnly(13, 0), new TimeOnly(15, 0), sem2From, sem2To, year: 2, sem: 2, credits: 20, level: 5);
        var csSwe = MakeModule(csBsc, "CS2004", "Software Engineering Practice", DayOfWeek.Tuesday,
            new TimeOnly(11, 0), new TimeOnly(13, 0), sem2From, sem2To, year: 2, sem: 2, credits: 20, level: 5);

        var seReq = MakeModule(seBsc, "SE1001", "Requirements Engineering", DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(11, 0), sem1From, sem1To, year: 1, sem: 1, credits: 20, level: 4);
        var seOop = MakeModule(seBsc, "SE1002", "Object-Oriented Programming", DayOfWeek.Wednesday,
            new TimeOnly(13, 0), new TimeOnly(15, 0), sem1From, sem1To, year: 1, sem: 1, credits: 20, level: 4);

        var seTest = MakeModule(seBsc, "SE1003", "Software Testing & QA", DayOfWeek.Tuesday,
            new TimeOnly(10, 0), new TimeOnly(12, 0), sem2From, sem2To, year: 1, sem: 2, credits: 20, level: 4);
        var seAgile = MakeModule(seBsc, "SE1004", "Agile & DevOps", DayOfWeek.Thursday,
            new TimeOnly(14, 0), new TimeOnly(16, 0), sem2From, sem2To, year: 1, sem: 2, credits: 20, level: 4);

        var msMl = MakeModule(msMsc, "DS7001", "Machine Learning", DayOfWeek.Monday,
            new TimeOnly(10, 0), new TimeOnly(12, 0), sem1From, sem1To, year: 1, sem: 1, credits: 30, level: 7);
        var msStat = MakeModule(msMsc, "DS7002", "Statistical Modelling", DayOfWeek.Wednesday,
            new TimeOnly(14, 0), new TimeOnly(16, 0), sem1From, sem1To, year: 1, sem: 1, credits: 30, level: 7);

        var msBd = MakeModule(msMsc, "DS7003", "Big Data Engineering", DayOfWeek.Tuesday,
            new TimeOnly(9, 0), new TimeOnly(11, 0), sem2From, sem2To, year: 1, sem: 2, credits: 30, level: 7);
        var msNlp = MakeModule(msMsc, "DS7004", "Natural Language Processing", DayOfWeek.Thursday,
            new TimeOnly(13, 0), new TimeOnly(15, 0), sem2From, sem2To, year: 1, sem: 2, credits: 30, level: 7);

        var allModules = new[]
        {
            csAlgo, csProg, csMath, csOs, csDb,
            csAi, csNet, csSec, csSwe,
            seReq, seOop, seTest, seAgile,
            msMl, msStat, msBd, msNlp
        };

        db.Modules.AddRange(allModules);
        await db.SaveChangesAsync();

        Assign(csAlgo, jt);
        Assign(csAlgo, on, "Seminar Tutor");
        Assign(csProg, jt);
        Assign(csProg, md, "Lab Tutor");
        Assign(csMath, on);
        Assign(csOs, md);
        Assign(csDb, ps);
        Assign(csAi, ps);
        Assign(csAi, jt, "Guest Lecturer");
        Assign(csNet, md);
        Assign(csSec, md);
        Assign(csSec, so, "Lab Tutor");
        Assign(csSwe, so);
        Assign(seReq, so);
        Assign(seOop, jt);
        Assign(seTest, ps);
        Assign(seAgile, so);
        Assign(msMl, ps);
        Assign(msMl, jt, "Seminar Tutor");
        Assign(msStat, on);
        Assign(msBd, md);
        Assign(msNlp, ps);

        await db.SaveChangesAsync();

        AddElements(csAlgo, "Algorithms & Data Structures - Week 1 lecture slides");
        AddElements(csProg, "Programming Fundamentals - lecture notes");
        AddElements(csMath, "Discrete Mathematics - lecture notes");
        AddElements(csOs, "Operating Systems - lecture slides");
        AddElements(csDb, "Database Systems - lecture notes");
        AddElements(csAi, "Artificial Intelligence - lecture slides");
        AddElements(csNet, "Computer Networks - lecture notes");
        AddElements(csSec, "Cybersecurity Fundamentals - lecture slides");
        AddElements(csSwe, "Software Engineering Practice - lecture notes");
        AddElements(seReq, "Requirements Engineering - lecture slides");
        AddElements(seOop, "Object-Oriented Programming - lecture notes");
        AddElements(seTest, "Software Testing - lecture slides");
        AddElements(seAgile, "Agile & DevOps - lecture notes");
        AddElements(msMl, "Machine Learning - lecture slides");
        AddElements(msStat, "Statistical Modelling - lecture notes");
        AddElements(msBd, "Big Data Engineering - lecture slides");
        AddElements(msNlp, "Natural Language Processing - lecture notes");

        await db.SaveChangesAsync();

        var csY1Students = new[]
        {
            MakeStudent("alice.chen", "Alice", "Chen", new DateOnly(2004, 6, 12), "u2300001", 2023, 9, "Female",
                "12 Oak Lane, London, SW1 1AA", "07700 900001"),
            MakeStudent("ben.okafor", "Ben", "Okafor", new DateOnly(2004, 3, 25), "u2300002", 2023, 9, "Male",
                "34 Maple St, London, E1 2BB", "07700 900002"),
            MakeStudent("chloe.martin", "Chloe", "Martin", new DateOnly(2003, 11, 8), "u2300003", 2023, 9, "Female",
                "7 Birch Ave, Manchester, M1 3CC", "07700 900003"),
            MakeStudent("daniel.kim", "Daniel", "Kim", new DateOnly(2004, 1, 30), "u2300004", 2023, 9, "Male",
                "89 Elm Road, Leeds, LS1 4DD", "07700 900004"),
            MakeStudent("emily.walsh", "Emily", "Walsh", new DateOnly(2003, 8, 14), "u2300005", 2023, 9, "Female",
                "5 Cedar Close, Bristol, BS1 5EE", "07700 900005"),
            MakeStudent("femi.adeyemi", "Femi", "Adeyemi", new DateOnly(2004, 5, 3), "u2300006", 2023, 9, "Male",
                "22 Pine Crescent, London, N1 6FF", "07700 900006"),
        };

        var csY2Students = new[]
        {
            MakeStudent("grace.li", "Grace", "Li", new DateOnly(2003, 2, 19), "u2200007", 2022, 9, "Female",
                "10 Willow Way, London, W1 7GG", "07700 900007"),
            MakeStudent("henry.patel", "Henry", "Patel", new DateOnly(2002, 10, 7), "u2200008", 2022, 9, "Male",
                "15 Ash Drive, Birmingham, B1 8HH", "07700 900008"),
            MakeStudent("isla.murphy", "Isla", "Murphy", new DateOnly(2003, 4, 22), "u2200009", 2022, 9, "Female",
                "3 Elm Gardens, London, SE1 9II", "07700 900009"),
            MakeStudent("jack.taylor", "Jack", "Taylor", new DateOnly(2002, 12, 1), "u2200010", 2022, 9, "Male",
                "67 Sycamore Rd, Oxford, OX1 1JJ", "07700 900010"),
        };

        var seY1Students = new[]
        {
            MakeStudent("karen.white", "Karen", "White", new DateOnly(2004, 7, 16), "u2300011", 2023, 9, "Female",
                "8 Hazel St, London, EC1 2KK", "07700 900011"),
            MakeStudent("liam.brown", "Liam", "Brown", new DateOnly(2004, 2, 28), "u2300012", 2023, 9, "Male",
                "44 Walnut Close, Leeds, LS2 3LL", "07700 900012"),
            MakeStudent("mia.johnson", "Mia", "Johnson", new DateOnly(2003, 9, 5), "u2300013", 2023, 9, "Female",
                "29 Chestnut Ave, Bristol, BS2 4MM", "07700 900013"),
            MakeStudent("noah.smith", "Noah", "Smith", new DateOnly(2004, 4, 11), "u2300014", 2023, 9, "Male",
                "11 Beech Lane, London, W2 5NN", "07700 900014"),
        };

        var mscStudents = new[]
        {
            MakeStudent("olivia.garcia", "Olivia", "Garcia", new DateOnly(1999, 1, 20), "u2500015", 2025, 9, "Female",
                "55 Poplar Rd, London, SE2 6OO", "07700 900015"),
            MakeStudent("peter.wilson", "Peter", "Wilson", new DateOnly(1998, 6, 3), "u2500016", 2025, 9, "Male",
                "18 Rowan Ave, Edinburgh, EH1 7PP", "07700 900016"),
            MakeStudent("quinn.evans", "Quinn", "Evans", new DateOnly(2000, 3, 15), "u2500017", 2025, 9, "Non-binary",
                "6 Larch Blvd, London, N2 8QQ", "07700 900017"),
        };

        var allStudentGroups = new[]
        {
            csY1Students, csY2Students, seY1Students, mscStudents
        };

        foreach (var group in allStudentGroups)
        {
            foreach (var (user, _, _) in group)
            {
                db.Users.Add(user);
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = studentRole.Id });
            }
        }

        await db.SaveChangesAsync();

        foreach (var group in allStudentGroups)
        {
            foreach (var (_, _, record) in group)
            {
                db.StudentRecords.Add(record);
            }
        }

        await db.SaveChangesAsync();

        foreach (var group in allStudentGroups)
        {
            foreach (var (_, student, _) in group)
            {
                db.Students.Add(student);
            }
        }

        await db.SaveChangesAsync();

        foreach (var (_, s, _) in csY1Students) EnrollCourse(s, csBsc, 1);
        foreach (var (_, s, _) in csY2Students) EnrollCourse(s, csBsc, 2);
        foreach (var (_, s, _) in seY1Students) EnrollCourse(s, seBsc, 1);
        foreach (var (_, s, _) in mscStudents) EnrollCourse(s, msMsc, 1);

        await db.SaveChangesAsync();

        foreach (var (_, s, _) in csY1Students)
        {
            EnrollModule(s, csAlgo, 1);
            EnrollModule(s, csProg, 1);
            EnrollModule(s, csMath, 1);
            EnrollModule(s, csOs, 1, semester: 2);
            EnrollModule(s, csDb, 1, semester: 2);
        }

        foreach (var (_, s, _) in csY2Students)
        {
            EnrollModule(s, csAi, 2);
            EnrollModule(s, csNet, 2);
            EnrollModule(s, csSec, 2, semester: 2);
            EnrollModule(s, csSwe, 2, semester: 2);
        }

        foreach (var (_, s, _) in seY1Students)
        {
            EnrollModule(s, seReq, 1);
            EnrollModule(s, seOop, 1);
            EnrollModule(s, seTest, 1, semester: 2);
            EnrollModule(s, seAgile, 1, semester: 2);
        }

        foreach (var (_, s, _) in mscStudents)
        {
            EnrollModule(s, msMl, 1);
            EnrollModule(s, msStat, 1);
            EnrollModule(s, msBd, 1, semester: 2);
            EnrollModule(s, msNlp, 1, semester: 2);
        }

        await db.SaveChangesAsync();

        var rng = new Random(42);

        var csY1Sem1Modules = new[] { csAlgo, csProg, csMath };
        foreach (var (_, s, _) in csY1Students)
        {
            foreach (var m in csY1Sem1Modules)
                SeedAttendance(s, m, rng.NextDouble() * 0.35 + 0.65);
        }

        var csY2Sem1Modules = new[] { csAi, csNet };
        foreach (var (_, s, _) in csY2Students)
        {
            foreach (var m in csY2Sem1Modules)
                SeedAttendance(s, m, rng.NextDouble() * 0.35 + 0.65);
        }

        var seY1Sem1Modules = new[] { seReq, seOop };
        foreach (var (_, s, _) in seY1Students)
        {
            foreach (var m in seY1Sem1Modules)
                SeedAttendance(s, m, rng.NextDouble() * 0.35 + 0.65);
        }

        var mscSem1Modules = new[] { msMl, msStat };
        foreach (var (_, s, _) in mscStudents)
        {
            foreach (var m in mscSem1Modules)
                SeedAttendance(s, m, rng.NextDouble() * 0.35 + 0.65);
        }

        await db.SaveChangesAsync();

        var sem1AssessmentModules = new[] { csAlgo, csProg, csMath, csAi, csNet, seReq, seOop, msMl, msStat };
        foreach (var mod in sem1AssessmentModules)
        {
            var assessmentElements = await db.ModuleElements
                .Where(e => e.ModuleId == mod.Id && e.Type == ModuleElementType.Assessment)
                .ToListAsync();

            var enrolledStudentIds = await db.StudentModuleEnrollments
                .Where(e => e.ModuleId == mod.Id)
                .Select(e => e.StudentId)
                .ToListAsync();

            var graderStaff = await db.ModuleStaff
                .Where(ms => ms.ModuleId == mod.Id)
                .Select(ms => ms.Staff)
                .FirstAsync();

            foreach (var element in assessmentElements)
            {
                element.MarksPublished = true;

                foreach (var sid in enrolledStudentIds)
                {
                    var grade = Math.Round(40 + rng.NextDouble() * 60, 1);
                    db.AssessmentGrades.Add(new AssessmentGrade
                    {
                        AssessmentElementId = element.Id,
                        StudentId = sid,
                        Grade = grade,
                        Feedback = grade >= 70 ? "Excellent work." :
                            grade >= 50 ? "Good effort, some areas to improve." :
                            "Please see tutor for additional support.",
                        GradedByStaffId = graderStaff.Id,
                        GradedAtUtc = DateTime.UtcNow.AddDays(-rng.Next(7, 30))
                    });
                }
            }
        }

        await db.SaveChangesAsync();
        return;

        void SeedAttendance(Student student, Module module, double attendanceRate)
        {
            var sessions = GetSessionDates(module.ScheduledDay, module.RunsFrom, module.RunsTo);
            foreach (var date in sessions.Where(date => !(rng.NextDouble() > attendanceRate)))
            {
                db.StudentAttendances.Add(new StudentAttendance
                {
                    StudentId = student.Id,
                    ModuleId = module.Id,
                    Date = date,
                    CheckedInAtUtc = new DateTimeOffset(date.Year, date.Month, date.Day,
                        module.ScheduledStartLocal.Hour, module.ScheduledStartLocal.Minute + rng.Next(0, 8), 0,
                        TimeSpan.Zero)
                });
            }
        }

        void EnrollModule(Student s, Module m, int yearOfStudy, int academicYear = 2025, short semester = 1) =>
            db.StudentModuleEnrollments.Add(new StudentModuleEnrollment
            {
                StudentId = s.Id,
                ModuleId = m.Id,
                AcademicYear = academicYear,
                YearOfStudy = yearOfStudy,
                Semester = semester,
                Status = ModuleEnrollmentStatus.Enrolled,
                EnrolledAtUtc = new DateTimeOffset(academicYear, 9, 22, 0, 0, 0, TimeSpan.Zero)
            });

        void EnrollCourse(Student s, Course c, int yearOfStudy, int academicYear = 2025, short semester = 1) =>
            db.StudentCourseEnrollments.Add(new StudentCourseEnrollment
            {
                StudentId = s.Id,
                CourseId = c.Id,
                AcademicYear = academicYear,
                YearOfStudy = yearOfStudy,
                Semester = semester,
                Status = CourseEnrollmentStatus.Active,
                StartDateUtc = new DateTimeOffset(academicYear, 9, 22, 0, 0, 0, TimeSpan.Zero)
            });

        void Assign(Module m, Staff s, string role = "Lecturer") =>
            db.ModuleStaff.Add(new ModuleStaff { ModuleId = m.Id, StaffId = s.Id, Role = role });
    }

    private static (User, Staff) MakeStaff(string emailPrefix, string first, string last,
        DateOnly dob, string staffNumber, string department)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Test123!?");

        const long staffRolePermissions = (long)(Permission.CatalogRead
                                                 | Permission.EnrollmentRead
                                                 | Permission.AttendanceRead
                                                 | Permission.AttendanceWrite);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{emailPrefix}@uni.com",
            PasswordHash = hash,
            FirstName = first,
            LastName = last,
            DateOfBirth = dob,
            IsActive = true,
            Permissions = staffRolePermissions
        };

        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StaffNumber = staffNumber,
            Department = department
        };

        return (user, staff);
    }

    private static (User, Student, StudentRecord) MakeStudent(string emailPrefix, string first, string last,
        DateOnly dob, string studentNumber, int startYear, int startMonth, string gender, string address, string phone)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Test123!?");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{emailPrefix}@uni.com",
            PasswordHash = hash,
            FirstName = first,
            LastName = last,
            DateOfBirth = dob,
            IsActive = true,
            Permissions = 0
        };
        var record = new StudentRecord
        {
            Id = Guid.NewGuid(),
            PersonalEmail = $"{emailPrefix}@gmail.com",
            HomeAddress = address,
            PhoneNumber = phone,
            Gender = gender,
            EntryQualifications = ["A-Level Mathematics (A)", "A-Level Computer Science (A)", "A-Level Physics (B)"]
        };
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StudentRecordId = record.Id,
            StudentNumber = studentNumber,
            Status = "active",
            StartYear = startYear,
            StartMonth = startMonth
        };
        return (user, student, record);
    }

    private static Module MakeModule(Course course, string code, string title,
        DayOfWeek day, TimeOnly start, TimeOnly end, DateOnly from, DateOnly to,
        int year, int sem, int credits, int level)
    {
        return new Module
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            ModuleCode = code,
            Title = title,
            Description = $"{title} — core module.",
            IsCore = true,
            AcademicYear = 2025,
            Credits = credits,
            Level = level,
            SemesterOfStudy = sem,
            ScheduledDay = day,
            ScheduledStartLocal = start,
            ScheduledEndLocal = end,
            RunsFrom = from,
            RunsTo = to
        };
    }

    private void AddElements(Module module, string notesTitle)
    {
        db.ModuleElements.Add(new ModuleElement
        {
            ModuleId = module.Id,
            SortOrder = 1,
            Type = ModuleElementType.Headline,
            IconKey = "info",
            Options = JsonDocument.Parse(JsonSerializer.Serialize(new { text = $"Welcome to {module.Title}" }))
        });

        db.ModuleElements.Add(new ModuleElement
        {
            ModuleId = module.Id,
            SortOrder = 2,
            Type = ModuleElementType.Text,
            Options = JsonDocument.Parse(JsonSerializer.Serialize(new { content = module.Description }))
        });

        db.ModuleElements.Add(new ModuleElement
        {
            ModuleId = module.Id,
            SortOrder = 3,
            Type = ModuleElementType.Assessment,
            IconKey = "assignment",
            AssessmentWeight = 40,
            MarksPublished = false,
            Options = JsonDocument.Parse(
                JsonSerializer.Serialize(new { title = "Coursework Assignment", maxMark = 100 }))
        });

        db.ModuleElements.Add(new ModuleElement
        {
            ModuleId = module.Id,
            SortOrder = 4,
            Type = ModuleElementType.Assessment,
            IconKey = "quiz",
            AssessmentWeight = 60,
            MarksPublished = false,
            Options = JsonDocument.Parse(JsonSerializer.Serialize(new { title = "Final Examination", maxMark = 100 }))
        });

        db.ModuleElements.Add(new ModuleElement
        {
            ModuleId = module.Id,
            SortOrder = 5,
            Type = ModuleElementType.Link,
            IconKey = "book",
            Options = JsonDocument.Parse(JsonSerializer.Serialize(new
                { url = "https://library.uni.com/reading-list", label = "Module Reading List" }))
        });
    }

    private static List<DateOnly> GetSessionDates(DayOfWeek day, DateOnly from, DateOnly to)
    {
        var dates = new List<DateOnly>();
        var current = from;
        while (current.DayOfWeek != day)
            current = current.AddDays(1);
        while (current <= to)
        {
            dates.Add(current);
            current = current.AddDays(7);
        }

        return dates;
    }

    private async Task SeedSystemRolesAsync()
    {
        var rolesExist = await db.Roles.AnyAsync();
        if (rolesExist) return;

        var studentRole = new Role
        {
            Key = "student",
            Name = "Student",
            Permissions = (long)Permission.None,
            Rank = 10,
            IsSystem = true
        };

        var staffRole = new Role
        {
            Key = "staff",
            Name = "Staff",
            Permissions = (long)(Permission.CatalogRead | Permission.EnrollmentRead | Permission.AttendanceRead |
                                 Permission.AttendanceWrite),
            Rank = 20,
            IsSystem = true
        };

        var adminRole = new Role
        {
            Key = "admin",
            Name = "Administrator",
            Permissions = (long)Permission.SuperAdmin,
            Rank = 999,
            IsSystem = true
        };

        db.Roles.AddRange(studentRole, staffRole, adminRole);
        await db.SaveChangesAsync();
    }
}
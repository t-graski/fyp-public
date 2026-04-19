using System.Text.Json;
using backend.models;
using backend.models.@base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace backend.data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public Guid? ActorUserId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestPath { get; set; }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleElement> ModuleElements => Set<ModuleElement>();
    public DbSet<ModuleStaff> ModuleStaff => Set<ModuleStaff>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<StudentCourseEnrollment> StudentCourseEnrollments => Set<StudentCourseEnrollment>();
    public DbSet<StudentModuleEnrollment> StudentModuleEnrollments => Set<StudentModuleEnrollment>();
    public DbSet<StudentAttendance> StudentAttendances => Set<StudentAttendance>();
    public DbSet<StudentRecord> StudentRecords => Set<StudentRecord>();
    public DbSet<AttendanceSettings> AttendanceSettings => Set<AttendanceSettings>();
    public DbSet<AssessmentGrade> AssessmentGrades => Set<AssessmentGrade>();
    public DbSet<ModuleFile> ModuleFiles => Set<ModuleFile>();
    public DbSet<ModuleContentChunk> ModuleContentChunks => Set<ModuleContentChunk>();
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("vector");
        
        base.OnModelCreating(b);

        // USERS
        b.Entity<User>(e =>
        {
            e.ToTable("users");

            e.HasKey(x => x.Id);

            e.Property(x => x.Email)
                .IsRequired();

            e.HasIndex(x => x.Email)
                .IsUnique();
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("roles");

            e.HasKey(x => x.Id);

            e.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(64);

            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(128);

            e.HasIndex(x => x.Key)
                .IsUnique();
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");

            e.HasKey(x => x.Id);

            e.HasOne(x => x.User)
                .WithMany(u => u.Roles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.UserId, x.RoleId })
                .IsUnique();
        });

        // STUDENT / STAFF
        b.Entity<Student>(e =>
        {
            e.ToTable("students");
            e.HasKey(x => x.Id);
            e.Property(x => x.StudentNumber).IsRequired();
            e.HasIndex(x => x.StudentNumber).IsUnique();

            e.HasOne(x => x.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.StudentRecord)
                .WithOne(r => r.Student)
                .HasForeignKey<Student>(x => x.StudentRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.StudentRecordId).IsUnique();
        });

        b.Entity<StudentRecord>(e =>
        {
            e.ToTable("student_records");

            e.HasKey(x => x.Id);

            e.Property(x => x.PersonalEmail);
            e.Property(x => x.HomeAddress);
            e.Property(x => x.PhoneNumber);
            e.Property(x => x.EntryQualifications);
            e.Property(x => x.Gender);
        });

        b.Entity<Staff>(e =>
        {
            e.ToTable("staff");
            e.HasKey(x => x.Id);
            e.Property(x => x.StaffNumber).IsRequired();
            e.HasIndex(x => x.StaffNumber).IsUnique();

            e.HasOne(x => x.User)
                .WithOne(u => u.Staff)
                .HasForeignKey<Staff>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // COURSE / MODULE
        b.Entity<Course>(e =>
        {
            e.ToTable("courses");
            e.HasKey(x => x.Id);
            e.Property(x => x.CourseCode).IsRequired();
            e.Property(x => x.Title).IsRequired();
            e.HasIndex(x => x.CourseCode).IsUnique();
        });

        b.Entity<Module>(e =>
        {
            e.ToTable("modules");
            e.HasKey(x => x.Id);

            e.Property(x => x.ModuleCode).IsRequired();
            e.Property(x => x.Title).IsRequired();

            e.Property(x => x.ScheduledDay).IsRequired();
            e.Property(x => x.ScheduledStartLocal).IsRequired();
            e.Property(x => x.ScheduledEndLocal).IsRequired();

            e.Property(x => x.IsCore).HasDefaultValue(true);

            e.Property(x => x.RunsFrom).IsRequired();
            e.Property(x => x.RunsTo).IsRequired();

            e.HasIndex(x => new { x.CourseId, x.ModuleCode }).IsUnique();

            e.HasOne(x => x.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Elements)
                .WithOne(x => x.Module)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ModuleElement>(e =>
        {
            e.ToTable("module_elements");
            e.HasKey(x => x.Id);

            e.Property(x => x.ModuleId).IsRequired();
            e.Property(x => x.SortOrder).IsRequired();
            e.Property(x => x.Type).IsRequired();

            e.Property(x => x.IconKey);

            e.Property(x => x.Options)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb");

            e.Property(x => x.AssessmentWeight);
            e.Property(x => x.MarksPublished).HasDefaultValue(false);

            e.HasIndex(x => new { x.ModuleId, x.SortOrder }).IsUnique();
            e.HasIndex(x => new { x.ModuleId, x.Type });

            e.HasOne(x => x.Module)
                .WithMany(m => m.Elements)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ModuleFile>(e =>
        {
            e.ToTable("module_files");
            e.Property(x => x.OriginalFileName).IsRequired();
            e.Property(x => x.StorageKey).IsRequired();
            e.HasIndex(x => x.ModuleId);
        });

        b.HasPostgresExtension("vector");

        b.Entity<ModuleContentChunk>(e =>
        {
            e.ToTable("module_content_chunks");

            e.HasKey(x => x.Id);
            e.Property(x => x.Content).IsRequired();

            e.Property(x => x.Embedding).HasColumnType("vector(1536)");

            e.HasIndex(x => x.ModuleId);
            e.HasIndex(x => x.ModuleElementId);
            e.HasIndex(x => x.ModuleFileId);
        });

        b.Entity<ChatThread>(e => { e.HasIndex(x => new { x.ModuleId, x.OwnerUserId }); });

        b.Entity<ChatMessage>(e => { e.HasIndex(x => x.ThreadId); });

        b.Entity<AssessmentGrade>(e =>
        {
            e.ToTable("assessment_grades");
            e.HasKey(x => x.Id);

            e.Property(x => x.AssessmentElementId).IsRequired();
            e.Property(x => x.StudentId).IsRequired();
            e.Property(x => x.Grade).IsRequired();
            e.Property(x => x.GradedByStaffId).IsRequired();
            e.Property(x => x.GradedAtUtc).IsRequired();

            e.HasIndex(x => new { x.AssessmentElementId, x.StudentId }).IsUnique();

            e.HasOne(x => x.AssessmentElement)
                .WithMany(el => el.AssessmentGrades)
                .HasForeignKey(x => x.AssessmentElementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AttendanceSettings>(e =>
        {
            e.ToTable("attendance_settings");

            e.HasKey(x => x.Id);

            e.Property(x => x.CheckInStartLocal).IsRequired();
            e.Property(x => x.CheckInEndLocal).IsRequired();
            e.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(100);
            e.Property(x => x.IsActive).IsRequired();
        });

        b.Entity<ModuleStaff>(e =>
        {
            e.ToTable("module_staff");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ModuleId, x.StaffId }).IsUnique();

            e.HasOne(x => x.Module)
                .WithMany(m => m.TeachingStaff)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Staff)
                .WithMany(s => s.ModuleStaff)
                .HasForeignKey(x => x.StaffId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasKey(x => x.AuditEventId);

            e.Property(x => x.Action).IsRequired().HasMaxLength(32);
            e.Property(x => x.EntityType).IsRequired().HasMaxLength(128);
            e.Property(x => x.EntityTable).IsRequired().HasMaxLength(128);
            e.Property(x => x.EntityId).IsRequired().HasMaxLength(256);

            e.Property(x => x.ChangesJson).HasColumnType("jsonb");
            e.Property(x => x.MetadataJson).HasColumnType("jsonb");

            e.HasIndex(x => x.OccurredAtUtc);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.ActorUserId);

            e.HasOne(x => x.ActorUser)
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // STUDENT COURSE ENROLLMENTS
        b.Entity<StudentCourseEnrollment>(e =>
        {
            e.ToTable("student_course_enrollments");
            e.HasKey(x => x.Id);

            e.Property(x => x.Status)
                .HasConversion<short>()
                .IsRequired();

            e.Property(x => x.AcademicYear).IsRequired();
            e.Property(x => x.YearOfStudy).IsRequired();
            e.Property(x => x.Semester).IsRequired();

            e.HasOne(x => x.Student)
                .WithMany(s => s.CourseEnrollments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Course)
                .WithMany(c => c.StudentEnrollments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.StudentId, x.CourseId, x.AcademicYear, x.Semester })
                .IsUnique();
        });

        // Student Module Enrollment
        b.Entity<StudentModuleEnrollment>(e =>
        {
            e.ToTable("student_module_enrollments");
            e.HasKey(x => x.Id);

            e.Property(x => x.Status)
                .HasConversion<short>()
                .IsRequired();

            e.Property(x => x.AcademicYear).IsRequired();
            e.Property(x => x.YearOfStudy).IsRequired();
            e.Property(x => x.Semester).IsRequired();

            e.HasOne(x => x.Student)
                .WithMany(s => s.ModuleEnrollments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Module)
                .WithMany(m => m.StudentEnrollments)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.StudentId, x.ModuleId, x.AcademicYear, x.Semester })
                .IsUnique();
        });

        // Student Attendance 
        b.Entity<StudentAttendance>(e =>
        {
            e.ToTable("student_attendance");
            e.HasKey(x => x.Id);

            e.Property(x => x.Date).IsRequired();
            e.Property(x => x.CheckedInAtUtc).IsRequired();

            e.HasIndex(x => new { x.StudentId, x.ModuleId, x.Date }).IsUnique();

            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = ActorUserId;

        var loginEntries = new HashSet<User>();
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Modified && IsLoginEvent(entry))
            {
                loginEntries.Add(entry.Entity);
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedByUserId = actorId;
            }

            if (entry.State == EntityState.Modified && IsSoftDelete(entry))
            {
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedByUserId = actorId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.Entity is User user && loginEntries.Contains(user))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId = actorId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = actorId;
            }
        }

        var auditEvents = BuildAuditEvents(now, RequestPath, CorrelationId, loginEntries);

        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEvents.Count > 0)
        {
            AuditEvents.AddRange(auditEvents);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static readonly HashSet<string> SensitiveProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "RefreshToken",
        "AccessToken",
        "Token",
        "Secret"
    };

    private List<AuditEvent> BuildAuditEvents(DateTimeOffset now, string? requestPath = null,
        string? correlationId = null, HashSet<User>? loginEntries = null)
    {
        var list = new List<AuditEvent>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditEvent) continue;
            if (entry.State is EntityState.Unchanged or EntityState.Detached) continue;

            if (entry.Metadata.IsOwned()) continue;

            var isLogin = entry.Entity is User user && (loginEntries?.Contains(user) ?? false);

            var action = isLogin
                ? "LOGIN"
                : entry.State switch
                {
                    EntityState.Added => "INSERT",
                    EntityState.Modified => IsSoftDelete(entry) ? "SOFT_DELETE" : "UPDATE",
                    EntityState.Deleted => "HARD_DELETE",
                    _ => entry.State.ToString().ToUpperInvariant()
                };

            var entityType = entry.Metadata.ClrType.Name;
            var table = entry.Metadata.GetTableName() ?? entityType;

            var keyString = GetPrimaryKeyString(entry);
            var changes = new Dictionary<string, AuditFieldChange>();

            if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.Properties)
                {
                    if (ShouldSkipProperty(prop.Metadata)) continue;

                    changes[prop.Metadata.Name] = new AuditFieldChange(null, prop.CurrentValue);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified) continue;
                    if (ShouldSkipProperty(prop.Metadata)) continue;

                    changes[prop.Metadata.Name] = new AuditFieldChange(prop.OriginalValue, prop.CurrentValue);
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var prop in entry.Properties)
                {
                    if (ShouldSkipProperty(prop.Metadata)) continue;

                    changes[prop.Metadata.Name] = new AuditFieldChange(prop.OriginalValue, null);
                }
            }

            if (changes.Count == 0) continue;

            var payload = new AuditChanges(changes);
            var metadata = new Dictionary<string, object>
            {
                ["path"] = requestPath,
                ["correlationId"] = correlationId
            };

            list.Add(new AuditEvent
            {
                OccurredAtUtc = now,
                ActorUserId = ActorUserId,
                Action = action,
                EntityType = entityType,
                EntityTable = table,
                EntityId = keyString,
                ChangesJson = JsonSerializer.Serialize(payload),
                MetadataJson = JsonSerializer.Serialize(metadata)
            });
        }

        return list;
    }

    private static bool IsLoginEvent(EntityEntry entry)
    {
        var modifiedProps = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToList();

        return modifiedProps is [nameof(User.LastLoginAtUtc)];
    }

    private static bool ShouldSkipProperty(IProperty prop)
    {
        if (prop.IsPrimaryKey()) return true;

        var name = prop.Name;
        if (SensitiveProps.Contains(name)) return true;

        return name is "CreatedAtUtc" or "CreatedByUserId" or "UpdatedAtUtc" or "UpdatedByUserId" or "DeletedAtUtc" or "DeletedByUserId" or "IsDeleted";
    }

    private static bool IsSoftDelete(EntityEntry entry)
    {
        if (entry.Entity is not ISoftDeletable) return false;
        var prop = entry.Property(nameof(ISoftDeletable.IsDeleted));
        return prop is { IsModified: true, CurrentValue: true };
    }

    private static string GetPrimaryKeyString(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk is null) return "UNKNOWN_KEY";
        var parts = pk.Properties.Select(p => $"{p.Name}={entry.Property(p.Name).CurrentValue}");
        return string.Join(";", parts);
    }
}
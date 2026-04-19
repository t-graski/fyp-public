using backend.models.@base;

namespace backend.models;

public class User : SoftDeletableEntity<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
    public long Permissions { get; set; }

    public DateTimeOffset? EmailVerifiedAtUtc { get; set; }
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockOutUntilUtc { get; set; }
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

    public Student? Student { get; set; }
    public Staff? Staff { get; set; }
}
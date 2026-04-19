using backend.models.@base;

namespace backend.models;

public class Staff : SoftDeletableEntity<Guid>
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string StaffNumber { get; set; } = string.Empty;

    public string? Department { get; set; }

    public ICollection<ModuleStaff> ModuleStaff { get; set; } = new List<ModuleStaff>();
}
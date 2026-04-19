using backend.models.@base;

namespace backend.models;

public class ModuleStaff : SoftDeletableEntity<Guid>
{
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;

    public string? Role { get; set; }
}
using backend.models.@base;

namespace backend.models;

public class AttendanceSettings : AuditableEntity<Guid>
{
    public TimeOnly CheckInStartLocal { get; set; }
    public TimeOnly CheckInEndLocal { get; set; }

    public string TimeZoneId { get; set; } = "Europe/London";

    public bool IsActive { get; init; } = true;
}
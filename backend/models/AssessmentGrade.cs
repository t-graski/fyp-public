using backend.models.@base;

namespace backend.models;

public class AssessmentGrade : SoftDeletableEntity<Guid>
{
    public Guid AssessmentElementId { get; init; }
    public ModuleElement AssessmentElement { get; init; } = null;

    public Guid StudentId { get; set; }
    public double Grade { get; set; }
    public string? Feedback { get; set; }
    
    public Guid GradedByStaffId { get; set; }
    public DateTime GradedAtUtc { get; set; }
}
using System.Text.Json;
using backend.models.@base;
using backend.models.enums;

namespace backend.models;

public class ModuleElement : SoftDeletableEntity<Guid>
{
   public Guid ModuleId { get; init; } 
   public Module Module { get; init; }
   
   public int SortOrder { get; set; }
   public ModuleElementType Type { get; init; }
   
   public string? IconKey { get; set; }

   public JsonDocument Options { get; set; } = JsonDocument.Parse("{}");
   
   public double? AssessmentWeight { get; set; }
   public bool MarksPublished { get; set; } = false;

   public ICollection<AssessmentGrade> AssessmentGrades { get; init; } = [];
}
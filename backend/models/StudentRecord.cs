using backend.models.@base;

namespace backend.models;

public class StudentRecord : SoftDeletableEntity<Guid>
{
   public Student Student { get; set; }
   public string PersonalEmail { get; set; } = string.Empty;
   public string HomeAddress { get; set; } = string.Empty;
   public string PhoneNumber { get; set; } = string.Empty;
   public ICollection<string> EntryQualifications { get; set; } = [];
   public string Gender { get; set; }
}
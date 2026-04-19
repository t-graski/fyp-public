using backend.models.@base;

namespace backend.models;

public class ChatThread : SoftDeletableEntity<Guid>
{
   public Guid ModuleId { get; set; }
   public Module Module { get; set; } = null!;
   
   public Guid OwnerUserId { get; set; }

   public string Title { get; set; } = "New chat";

   public ICollection<ChatMessage> Messages { get; set; } = [];
}
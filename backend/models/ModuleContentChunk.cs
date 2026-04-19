using System.ComponentModel.DataAnnotations.Schema;
using backend.models.@base;
using Pgvector;

namespace backend.models;

public class ModuleContentChunk : SoftDeletableEntity<Guid>
{
   public Guid ModuleId { get; set; }
   public Module Module { get; set; } = null;

   public Guid? ModuleElementId { get; set; }
   public Guid? ModuleFileId { get; set; }

   public string SourceType { get; set; } = "module_element";
   public string? Title { get; set; }
   
   public int? PageNumber { get; set; }
   public string Content { get; set; } = string.Empty;

   [Column(TypeName = "vector(1536)")]
   public Vector Embedding { get; set; } = null;
}
using backend.models.@base;

namespace backend.models;

public class ModuleFile : SoftDeletableEntity<Guid>
{
    public Guid ModuleId { get; set; }
    public Module Module { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }

    public string StorageKey { get; set; } = string.Empty;
}
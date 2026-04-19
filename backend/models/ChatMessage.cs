using System.Text.Json;
using backend.models.@base;
using backend.models.enums;

namespace backend.models;

public class ChatMessage : SoftDeletableEntity<Guid>
{
    public Guid ThreadId { get; init; }
    public ChatThread Thread { get; init; } = null!;

    public ChatRole Role { get; init; }
    public string Content { get; init; } = string.Empty;

    public JsonDocument? SourcesJson { get; init; }
}
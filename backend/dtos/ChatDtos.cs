namespace backend.dtos;

public record CreateThreadResponse(Guid ThreadId);

public record ThreadSummaryDto(Guid Id, string Title, DateTimeOffset UpdatedAtUtc);

public record SendMessageRequest(string Message);

public record SendMessageResponse(
    Guid AssistantMessageId,
    string Answer,
    List<RagSourceDto> Sources,
    Guid? HighlightElementId
);

public record ChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAtUtc,
    List<RagSourceDto>? Sources
);
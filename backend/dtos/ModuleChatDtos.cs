namespace backend.dtos;

public record RagIndexRequest(bool Rebuild = false);

public record RagChatRequest(string Message, int TopK = 6);

public record RagSourceDto(
    string SourceType,
    Guid? ModuleElementId,
    Guid? ModuleFileId,
    string? Title,
    int? PageNumber,
    string Snippet);

public record RagChatResponse(string Answer, List<RagSourceDto> Sources, Guid? HighlightElementId);
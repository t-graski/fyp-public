using backend.dtos;

namespace backend.services.interfaces;

public interface IModuleChatService
{
    Task<Guid> CreateThreadAsync(Guid moduleId, CancellationToken ct);
    Task<List<ThreadSummaryDto>> GetThreadsASync(Guid moduleId, CancellationToken ct);
    Task<List<ChatMessageDto>> GetMessagesAsync(Guid moduleId, Guid threadId, int take, CancellationToken ct);
    Task<SendMessageResponse> SendAsync(Guid moduleId, Guid threadId, string message, CancellationToken ct);
}
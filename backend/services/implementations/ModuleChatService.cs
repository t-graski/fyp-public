using System.Text.Json;
using backend.auth;
using backend.data;
using backend.dtos;
using backend.errors;
using backend.helpers.interfaces;
using backend.models;
using backend.models.enums;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class ModuleChatService(AppDbContext db, IModuleRagService rag, IDatabaseGuards guards) : IModuleChatService
{
    public async Task<Guid> CreateThreadAsync(Guid moduleId, CancellationToken ct)
    {
        var thread = new ChatThread
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OwnerUserId = await guards.GetUserIdOrThrowAsync(),
            Title = "New chat",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.ChatThreads.Add(thread);
        await db.SaveChangesAsync(ct);

        return thread.Id;
    }

    public async Task<List<ThreadSummaryDto>> GetThreadsASync(Guid moduleId, CancellationToken ct)
    {
        var meId = await guards.GetUserIdOrThrowAsync();
        
        return await db.ChatThreads
            .AsNoTracking()
            .Where(t => t.ModuleId == moduleId && t.OwnerUserId == meId && !t.IsDeleted)
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Take(3)
            .Select(t => new ThreadSummaryDto(t.Id, t.Title, t.UpdatedAtUtc ?? DateTimeOffset.UtcNow))
            .ToListAsync(ct);
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(Guid moduleId, Guid threadId, int take,
        CancellationToken ct)
    {
        var meId = await guards.GetUserIdOrThrowAsync();
        var threadExists = await db.ChatThreads
            .AsNoTracking()
            .AnyAsync(t => t.Id == threadId && t.ModuleId == moduleId && t.OwnerUserId == meId && !t.IsDeleted, ct);

        if (!threadExists)
        {
            throw new AppException(404, "THREAD_NOT_FOUND", "Chat thread not found.");
        }

        var messages = await db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ThreadId == threadId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(take)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

        return messages.Select(m => new ChatMessageDto(
            m.Id,
            m.Role == ChatRole.User ? "user" : "assistant",
            m.Content,
            m.CreatedAtUtc,
            m.SourcesJson is null
                ? null
                : JsonSerializer.Deserialize<List<RagSourceDto>>(m.SourcesJson.RootElement.GetRawText())
        )).ToList();
    }

    public async Task<SendMessageResponse> SendAsync(Guid moduleId, Guid threadId, string message, CancellationToken ct)
    {
        var meId = await guards.GetUserIdOrThrowAsync();
        var thread = await db.ChatThreads
            .FirstOrDefaultAsync(
                t => t.Id == threadId && t.ModuleId == moduleId && t.OwnerUserId == meId && !t.IsDeleted, ct);

        if (thread is null)
        {
            throw new AppException(404, "THREAD_NOT_FOUND", "Chat thread not found.");
        }

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            Role = ChatRole.User,
            Content = message,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.ChatMessages.Add(userMessage);
        thread.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        var ragResponse = await rag.ChatAsync(moduleId, message, topK: 6, ct);

        var sourcesJson = JsonDocument.Parse(JsonSerializer.Serialize(ragResponse.Sources));

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            Role = ChatRole.Assistant,
            Content = ragResponse.Answer,
            SourcesJson = sourcesJson,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.ChatMessages.Add(assistantMessage);
        thread.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (thread.Title == "New chat")
        {
            thread.Title = message.Length > 40 ? message[..40] + "..." : message;
        }

        await db.SaveChangesAsync(ct);

        return new SendMessageResponse(assistantMessage.Id, ragResponse.Answer, ragResponse.Sources,
            ragResponse.HighlightElementId);
    }
}

using Pgvector;

namespace backend.services.interfaces;

public interface IOpenAiService
{
    Task<Vector> EmbedAsync(string input, CancellationToken ct);
    Task<string> ChatAsync(string system, string user, CancellationToken ct);
}
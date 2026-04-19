namespace backend.services.interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct);
    Task<(Stream stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken ct);
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct);
}
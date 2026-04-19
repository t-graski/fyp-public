using backend.services.interfaces;

namespace backend.services.implementations;

public class LocalFileStorage(string rootPath) : IFileStorage
{
    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct)
    {
        Directory.CreateDirectory(rootPath);

        var key = Guid.NewGuid().ToString("N");
        var path = Path.Combine(rootPath, key);

        await using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
            useAsync: true);
        await content.CopyToAsync(fs, ct);

        return key;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct)
    {
        var path = Path.Combine(rootPath, storageKey);
        return Task.FromResult(File.Exists(path));
    }

    public Task<(Stream stream, string ContentType)> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var path = Path.Combine(rootPath, storageKey);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Storage object not found.", path);
        }

        Stream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult((s, "application/octet-stream"));
    }
}
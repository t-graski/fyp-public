namespace backend.dtos.bulk;

public record BulkCreateUsersRequest(
    bool DryRun = false,
    bool Atomic = false,
    bool ContinueOnError = true,
    int BatchSize = 200,
    IReadOnlyList<BulkItem<AdminCreateUserDto>> Items = null!
);

public record BulkItem<T>(string Key, T Data);

public record BulkItemResult<T>(
    string Key,
    bool Success,
    string? ErrorCode = null,
    string? Message = null,
    T? Data = default
);

public record BulkResult<T>(
    int Total,
    int Succeeded,
    int Failed,
    IReadOnlyList<BulkItemResult<T>> Items
);
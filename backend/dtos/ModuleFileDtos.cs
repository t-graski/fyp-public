namespace backend.dtos;

public record ModuleFileDto(
    Guid Id,
    Guid ModuleId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes
);

public record ModuleFileExistsDto(
    Guid FileId,
    bool Exists
);
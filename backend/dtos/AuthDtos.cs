using backend.models.@base;

namespace backend.dtos;

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    SystemRole Role);

public record LoginDto(string Email, string Password);

public record AuthResultDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    long Permissions,
    string AccessToken);
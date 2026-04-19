using backend.models;
using backend.services.interfaces;

namespace Backend.IntegrationTests.Fakes;

public sealed class FakeTokenService : ITokenService
{
    public string CreateAccessToken(User user) => $"TEST_TOKEN_FOR_{user.Id}";
}
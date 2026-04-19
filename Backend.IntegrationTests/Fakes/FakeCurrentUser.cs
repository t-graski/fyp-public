using backend.auth;

namespace Backend.IntegrationTests.Fakes;

public class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; init; }
    public bool IsAuthenticated { get; init; }
    public long PermissionBits { get; init; }
}
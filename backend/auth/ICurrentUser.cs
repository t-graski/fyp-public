namespace backend.auth;

public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    long PermissionBits { get; }
}
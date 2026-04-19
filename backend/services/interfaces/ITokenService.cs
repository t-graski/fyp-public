using backend.models;

namespace backend.services.interfaces;

public interface ITokenService
{
    string CreateAccessToken(User user);
}
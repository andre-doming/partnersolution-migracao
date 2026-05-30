namespace Partner.Api.Features.Auth;

public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool Verify(string rawPassword, string storedHash);
    bool IsModernHash(string storedHash);
}


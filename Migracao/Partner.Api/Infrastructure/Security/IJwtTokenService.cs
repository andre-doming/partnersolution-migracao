using Partner.Api.Features.Auth;

namespace Partner.Api.Infrastructure.Security;

public interface IJwtTokenService
{
    string GenerateToken(AuthUser user, IReadOnlyCollection<string> permissions, IReadOnlyCollection<int> companies);
}

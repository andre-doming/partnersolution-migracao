using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Partner.Api.Features.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Partner.Api.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(AuthUser user, IReadOnlyCollection<string> permissions, IReadOnlyCollection<int> companies)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(PartnerClaimTypes.UserId, user.Id.ToString()),
            new(PartnerClaimTypes.Login, user.Login),
            new(PartnerClaimTypes.Name, user.Name),
            new(PartnerClaimTypes.Admin, user.IsAdmin ? "true" : "false")
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(PartnerClaimTypes.Permissions, permission));
        }

        foreach (var companyId in companies)
        {
            claims.Add(new Claim(PartnerClaimTypes.Companies, companyId.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

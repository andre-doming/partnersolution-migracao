using Microsoft.AspNetCore.Http;
using Partner.Api.Features.Auth;
using Partner.Api.Features.Auth.Mfa;
using Partner.Api.Infrastructure.RateLimiting;
using Partner.Api.Infrastructure.Security;

namespace Partner.Api.Tests.RateLimiting;

public sealed class RateLimitingPartitionResolverTests
{
    [Fact]
    public void ResolveLogin_ShouldNormalizeLogin()
    {
        var context = new DefaultHttpContext();
        var request = new AuthLoginRequest { Login = "  User@Email.Com  " };
        context.Items[RateLimitingMetadataKeys.RequestBody] = System.Text.Json.JsonSerializer.Serialize(request);

        var result = RateLimitingPartitionResolver.ResolveLogin(context);

        Assert.Equal("user@email.com", result);
    }

    [Fact]
    public void ResolvePendingTokenId_ShouldExtractId()
    {
        var context = new DefaultHttpContext();
        var request = new MfaVerifyRequest { PendingToken = "11111111-1111-1111-1111-111111111111.token" };
        context.Items[RateLimitingMetadataKeys.RequestBody] = System.Text.Json.JsonSerializer.Serialize(request);

        var result = RateLimitingPartitionResolver.ResolvePendingTokenId(context);

        Assert.Equal("11111111-1111-1111-1111-111111111111", result);
    }

    [Fact]
    public void ResolveUserId_ShouldPreferClaim()
    {
        var context = new DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(PartnerClaimTypes.UserId, "42")
            }, "test"))
        };

        var result = RateLimitingPartitionResolver.ResolveUserId(context);

        Assert.Equal("42", result);
    }
}
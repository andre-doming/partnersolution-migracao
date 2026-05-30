using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Partner.Api.Features.Auth;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Shared.Extensions;
using Partner.Api.Tests.TestHost;
using System.Security.Claims;

namespace Partner.Api.Tests.Auth;

public sealed class PermissionPolicyTests
{
    [Fact]
    public async Task ImportPolicy_WithoutPermission_ShouldFail()
    {
        var serviceProvider = BuildServices();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(PartnerClaimTypes.UserId, "10"),
            new Claim(PartnerClaimTypes.Admin, "false")
        }, "test"));

        var result = await authorizationService.AuthorizeAsync(user, null, AuthPolicies.Import);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ImportPolicy_WithPermission_ShouldSucceed()
    {
        var serviceProvider = BuildServices();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(PartnerClaimTypes.UserId, "10"),
            new Claim(PartnerClaimTypes.Admin, "false"),
            new Claim(PartnerClaimTypes.Permissions, AuthPermissions.Import)
        }, "test"));

        var result = await authorizationService.AuthorizeAsync(user, null, AuthPolicies.Import);

        Assert.True(result.Succeeded);
    }

    private static ServiceProvider BuildServices()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PartnerDb"] = "Server=localhost;Database=db_partner;User Id=usr_partner;Password=Pass@2026!!!;TrustServerCertificate=True;",
            ["Jwt:Issuer"] = "Partner.Api",
            ["Jwt:Audience"] = "Partner.Web",
            ["Jwt:SecretKey"] = "DEV_ONLY_STRONG_SECRET_KEY_2026_ABCDEF_123456",
            ["Jwt:ExpirationMinutes"] = "60"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPartnerFoundation(configuration, new TestHostEnvironment());
        return services.BuildServiceProvider();
    }
}


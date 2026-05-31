using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partner.Api.Infrastructure.RateLimiting;
using Partner.Api.Shared.Extensions;
using Partner.Api.Tests.TestHost;

namespace Partner.Api.Tests.RateLimiting;

public sealed class RateLimitingPoliciesTests
{
    [Fact]
    public void AddPartnerFoundation_ShouldRegisterRateLimitingPolicies()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "Partner.Api",
                ["Jwt:Audience"] = "Partner.Web",
                ["Jwt:SecretKey"] = new string('a', 32),
                ["Jwt:ExpirationMinutes"] = "60",
                ["ConnectionStrings:PartnerDb"] = "Server=.;Database=fake;Trusted_Connection=True;"
            })
            .Build();

        services.AddPartnerFoundation(configuration, new TestHostEnvironment());

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<RateLimitingPolicyRegistry>();

        Assert.Contains(RateLimitingPolicyNames.AuthLogin, registry.All);
        Assert.Contains(RateLimitingPolicyNames.AuthMfaVerify, registry.All);
        Assert.Contains(RateLimitingPolicyNames.AuthMfaActivate, registry.All);
        Assert.Contains(RateLimitingPolicyNames.AuthMfaSetup, registry.All);
        Assert.Contains(RateLimitingPolicyNames.AdminMfaReset, registry.All);
        Assert.Contains(RateLimitingPolicyNames.AdminMfaUnlock, registry.All);
    }
}
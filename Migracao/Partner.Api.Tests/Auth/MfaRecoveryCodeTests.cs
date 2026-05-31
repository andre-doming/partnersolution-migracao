using Partner.Api.Features.Auth.Mfa;

namespace Partner.Api.Tests.Auth;

public sealed class MfaRecoveryCodeTests
{
    [Fact]
    public void RecoveryCode_ShouldRoundtripHash()
    {
        var service = new RecoveryCodeService();
        var code = service.GenerateCodes(1).Single();
        var hash = service.HashCode(code);

        var valid = service.VerifyCode(code, hash);
        var invalid = service.VerifyCode("XXXX-XXXX-XXXX", hash);

        Assert.True(valid);
        Assert.False(invalid);
    }
}
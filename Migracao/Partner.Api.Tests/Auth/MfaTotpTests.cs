using Partner.Api.Features.Auth.Mfa;

namespace Partner.Api.Tests.Auth;

public sealed class MfaTotpTests
{
    [Fact]
    public void GenerateSecret_ShouldReturnNonEmptyBytes()
    {
        var secret = MfaSecrets.GenerateSecret();

        Assert.NotNull(secret);
        Assert.NotEmpty(secret);
    }

    [Fact]
    public void Totp_ShouldValidateWithinWindow()
    {
        var secret = MfaSecrets.GenerateSecret();
        var now = DateTime.UtcNow;
        var code = TotpGenerator.GenerateCode(secret, now);

        var isValid = TotpGenerator.ValidateCode(secret, code, now, allowedDriftWindows: 1);

        Assert.True(isValid);
    }

    [Fact]
    public void Totp_ShouldFailForDifferentSecret()
    {
        var secret = MfaSecrets.GenerateSecret();
        var other = MfaSecrets.GenerateSecret();
        var now = DateTime.UtcNow;
        var code = TotpGenerator.GenerateCode(secret, now);

        var isValid = TotpGenerator.ValidateCode(other, code, now, allowedDriftWindows: 1);

        Assert.False(isValid);
    }
}
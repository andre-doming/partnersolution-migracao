using Partner.Api.Features.Auth;

namespace Partner.Api.Tests.Auth;

public sealed class AuthSecurityTests
{
    [Fact]
    public void Login_ModernHash_ShouldValidateSuccessfully()
    {
        var hasher = new PasswordHasher();
        var password = "Str0ng_Pass_2026!";
        var hash = hasher.Hash(password);

        var isValid = hasher.Verify(password, hash);

        Assert.True(isValid);
        Assert.True(hasher.IsModernHash(hash));
    }

    [Fact]
    public void Login_InvalidPassword_ShouldFailValidation()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("correct-password");

        var isValid = hasher.Verify("wrong-password", hash);

        Assert.False(isValid);
    }

    [Fact]
    public void Md5Legacy_ShouldValidate_AndModernHashShouldBeGeneratedForRehashPath()
    {
        const string password = "123456";
        const string md5LegacyHash = "e10adc3949ba59abbe56e057f20f883e";

        var legacyValid = PasswordVerifier.VerifyLegacyMd5(password, md5LegacyHash);

        var hasher = new PasswordHasher();
        var modernHash = hasher.Hash(password);

        Assert.True(legacyValid);
        Assert.True(hasher.IsModernHash(modernHash));
        Assert.True(hasher.Verify(password, modernHash));
    }
}


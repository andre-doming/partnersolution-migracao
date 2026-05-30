namespace Partner.Api.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "Partner.Api";
    public string Audience { get; init; } = "Partner.Web";
    public string SecretKey { get; init; } = "CHANGE_ME_MIN_32_CHARS_FOR_DEV_ONLY_12345";
    public int ExpirationMinutes { get; init; } = 60;
}

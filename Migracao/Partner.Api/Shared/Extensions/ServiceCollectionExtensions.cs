using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Partner.Api.Features.Auth;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;
using System.Security.Claims;
using System.Text;

namespace Partner.Api.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPartnerFoundation(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var partnerDbConnectionString = configuration.GetConnectionString("PartnerDb");
        EnsureConnectionStringConfigurationIsSafe(partnerDbConnectionString);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        EnsureJwtConfigurationIsSafe(jwtOptions, environment);

        var secretKey = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.Users, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.UsersView)));

            options.AddPolicy(AuthPolicies.UsersInsert, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.UsersInsert)));

            options.AddPolicy(AuthPolicies.UsersUpdate, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.UsersUpdate)));

            options.AddPolicy(AuthPolicies.UsersDelete, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.UsersDelete)));

            options.AddPolicy(AuthPolicies.Companies, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.CompaniesView)));

            options.AddPolicy(AuthPolicies.CompaniesInsert, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.CompaniesInsert)));

            options.AddPolicy(AuthPolicies.CompaniesUpdate, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.CompaniesUpdate)));

            options.AddPolicy(AuthPolicies.CompaniesDelete, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.CompaniesDelete)));

            options.AddPolicy(AuthPolicies.Clients, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.ClientsView)));

            options.AddPolicy(AuthPolicies.ClientsInsert, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.ClientsInsert)));

            options.AddPolicy(AuthPolicies.ClientsUpdate, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.ClientsUpdate)));

            options.AddPolicy(AuthPolicies.ClientsDelete, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.ClientsDelete)));

            options.AddPolicy(AuthPolicies.Import, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.Import)));
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var corsOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy("PartnerWeb", policy =>
            {
                policy
                    .WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sqlserver");
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddValidatorsFromAssemblyContaining<AuthLoginRequestValidator>(ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }

    private static bool HasAdminOrPermission(ClaimsPrincipal user, string permission)
    {
        var isAdmin = user.Claims.Any(c =>
            c.Type == PartnerClaimTypes.Admin &&
            string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase));

        if (isAdmin)
        {
            return true;
        }

        return user.Claims.Any(c => c.Type == PartnerClaimTypes.Permissions && string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureJwtConfigurationIsSafe(JwtOptions options, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(options.SecretKey) || options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey must have at least 32 chars.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        if (options.ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:ExpirationMinutes must be greater than zero.");
        }

        if (options.SecretKey.Contains("__REQUIRED_FROM_ENV__", StringComparison.OrdinalIgnoreCase)
            || options.SecretKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Jwt:SecretKey must be provided via environment variable and cannot use placeholder/default values.");
        }

        if (!environment.IsDevelopment() && options.SecretKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Jwt:SecretKey default value is not allowed outside Development.");
        }
    }

    private static void EnsureConnectionStringConfigurationIsSafe(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:PartnerDb is required and must be provided via environment variable.");
        }

        if (connectionString.Contains("__REQUIRED_FROM_ENV__", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ConnectionStrings:PartnerDb must be provided via environment variable and cannot use placeholder/default values.");
        }
    }
}

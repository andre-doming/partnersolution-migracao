using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Partner.Api.Infrastructure.RateLimiting;
using Partner.Api.Features.Auth;
using Partner.Api.Features.Auth.Mfa;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Import;
using Partner.Api.Infrastructure.Integrations.Vtex;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Middleware;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace Partner.Api.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPartnerFoundation(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var partnerDbConnectionString = configuration.GetConnectionString("PartnerDb");
        EnsureConnectionStringConfigurationIsSafe(partnerDbConnectionString);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MfaOptions>(configuration.GetSection(MfaOptions.SectionName));
        services.Configure<PasswordLockoutOptions>(configuration.GetSection(PasswordLockoutOptions.SectionName));
        services.Configure<CpfOptions>(configuration.GetSection("Cpf"));
        services.Configure<ImportRabbitMqOptions>(configuration.GetSection(ImportRabbitMqOptions.SectionName));
        services.Configure<ImportStorageOptions>(configuration.GetSection(ImportStorageOptions.SectionName));
        services.Configure<VtexOptions>(configuration.GetSection(VtexOptions.SectionName));
        services.Configure<VtexRabbitMqOptions>(configuration.GetSection(VtexRabbitMqOptions.SectionName));

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

            options.AddPolicy(AuthPolicies.UsersMfaAdmin, policy =>
                policy.RequireAssertion(context => HasAdminOrPermission(context.User, AuthPermissions.UsersMfaAdmin)));

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

        var policyRegistry = new RateLimitingPolicyRegistry();
        policyRegistry.Register(RateLimitingPolicyNames.AuthLogin);
        policyRegistry.Register(RateLimitingPolicyNames.AuthMfaVerify);
        policyRegistry.Register(RateLimitingPolicyNames.AuthMfaActivate);
        policyRegistry.Register(RateLimitingPolicyNames.AuthMfaSetup);
        policyRegistry.Register(RateLimitingPolicyNames.AdminMfaReset);
        policyRegistry.Register(RateLimitingPolicyNames.AdminMfaUnlock);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = (int)Math.Max(0, Math.Ceiling(retryAfter.TotalSeconds));
                    if (seconds > 0)
                    {
                        context.HttpContext.Response.Headers.RetryAfter = seconds.ToString();
                    }
                }

                var policy = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<RateLimitPolicyMetadata>()?.PolicyName;
                if (!string.IsNullOrWhiteSpace(policy))
                {
                    var metrics = context.HttpContext.RequestServices.GetRequiredService<RateLimitingMetrics>();
                    metrics.Increment(policy);
                }

                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimiting");
                logger.LogWarning(
                    "Rate limit hit. Endpoint={Endpoint} Policy={Policy} CorrelationId={CorrelationId} RateLimitHit={RateLimitHit}",
                    context.HttpContext.Request.Path.Value,
                    policy ?? "unknown",
                    CorrelationIdMiddleware.GetCorrelationId(context.HttpContext),
                    true);

                await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests." }, cancellationToken: token);
            };

            options.AddPolicy(RateLimitingPolicyNames.AuthLogin, context =>
            {
                var partitionKey = $"ip:{RateLimitingPartitionResolver.ResolveIp(context)}|login:{RateLimitingPartitionResolver.ResolveLogin(context)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitingPolicyNames.AuthMfaVerify, context =>
            {
                var partitionKey = $"ip:{RateLimitingPartitionResolver.ResolveIp(context)}|pending:{RateLimitingPartitionResolver.ResolvePendingTokenId(context)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitingPolicyNames.AuthMfaActivate, context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"user:{RateLimitingPartitionResolver.ResolveUserId(context)}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitingPolicyNames.AuthMfaSetup, context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"user:{RateLimitingPartitionResolver.ResolveUserId(context)}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitingPolicyNames.AdminMfaReset, context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"admin:{RateLimitingPartitionResolver.ResolveUserId(context)}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitingPolicyNames.AdminMfaUnlock, context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"admin:{RateLimitingPartitionResolver.ResolveUserId(context)}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
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
        services.AddScoped<ICpfProtectionService, CpfProtectionService>();
        services.AddScoped<RecoveryCodeService>();
        services.AddScoped<PendingTokenService>();
        services.AddSingleton<RateLimitingMetrics>();
        services.AddSingleton(policyRegistry);
        services.AddValidatorsFromAssemblyContaining<AuthLoginRequestValidator>(ServiceLifetime.Scoped, includeInternalTypes: true);
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IConfiguration>()
                .GetSection(ImportRabbitMqOptions.SectionName)
                .Get<ImportRabbitMqOptions>() ?? new ImportRabbitMqOptions();
            return new ImportRabbitMqConnectionFactory(options);
        });
        services.AddScoped<ImportJobRepository>();
        services.AddHostedService<ImportWorker>();

        // Registrar infraestrutura VTEX
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IConfiguration>()
                .GetSection(VtexRabbitMqOptions.SectionName)
                .Get<VtexRabbitMqOptions>() ?? new VtexRabbitMqOptions();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<VtexRabbitMqConnectionFactory>();
            return new VtexRabbitMqConnectionFactory(options, logger);
        });

        // Configurar HttpClient com resiliência para VTEX
        services.AddHttpClient<IVtexClient, VtexClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<VtexOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            client.Timeout = TimeSpan.FromSeconds(30);

            // Headers de autenticação VTEX (não hardcodados, apenas se configurados)
            if (!string.IsNullOrWhiteSpace(options.AppKey))
            {
                client.DefaultRequestHeaders.Add("X-VTEX-API-AppKey", options.AppKey);
            }

            if (!string.IsNullOrWhiteSpace(options.AppToken))
            {
                client.DefaultRequestHeaders.Add("X-VTEX-API-AppToken", options.AppToken);
            }
        });

        services.AddHostedService<VtexSyncWorker>();

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

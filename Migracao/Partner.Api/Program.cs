using Partner.Api.Features.Auth;
using Partner.Api.Features.Auth.Mfa;
using Partner.Api.Features.Clients;
using Partner.Api.Features.Companies;
using Partner.Api.Features.Import;
using Partner.Api.Features.Users;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Health;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Middleware;
using Partner.Api.Shared.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog.Events;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddPartnerFoundation(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, exception) =>
    {
        if (exception is not null || httpContext.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode >= 400)
        {
            return LogEventLevel.Warning;
        }

        return LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);
        var userId = httpContext.User.FindFirst(PartnerClaimTypes.UserId)?.Value;
        var route = httpContext.GetEndpoint()?.DisplayName ?? httpContext.Request.Path.Value ?? string.Empty;

        diagnosticContext.Set("correlationId", correlationId);
        diagnosticContext.Set("route", route);
        diagnosticContext.Set("statusCode", httpContext.Response.StatusCode);
        diagnosticContext.Set("elapsedMs", RequestMetricsMiddleware.GetElapsedMs(httpContext));

        if (!string.IsNullOrWhiteSpace(userId))
        {
            diagnosticContext.Set("userId", userId);
        }
    };
});
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("PartnerWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteMinimalJsonAsync
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteMinimalJsonAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => string.Equals(check.Name, "sqlserver", StringComparison.OrdinalIgnoreCase),
    ResponseWriter = HealthResponseWriter.WriteMinimalJsonAsync
});
app.MapAuthEndpoints();
app.MapMfaEndpoints();
app.MapUserEndpoints();
app.MapCompanyEndpoints();
app.MapClientEndpoints();
app.MapImportEndpoints();

app.Run();

public partial class Program;

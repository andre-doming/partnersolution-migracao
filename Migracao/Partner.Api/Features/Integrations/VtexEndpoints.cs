using Partner.Api.Infrastructure.Integrations.Vtex;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Partner.Api.Features.Integrations;

/// <summary>
/// Endpoints para integração com VTEX.
/// Fase IMP-8A: apenas diagnóstico e verificação de conectividade.
/// </summary>
public static class VtexEndpoints
{
    public static IEndpointRouteBuilder MapVtexEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integrations/vtex")
            .WithTags("Integrations")
            .RequireAuthorization(AuthPolicies.Import);

        group.MapGet("/health", GetHealthCheckAsync)
            .WithName("VtexHealthCheck")
            .Produces<VtexHealthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    /// <summary>
    /// Verifica a saúde da conexão com VTEX.
    /// Realiza uma chamada de diagnóstico sem enviar dados.
    /// 
    /// Resposta quando VTEX está habilitado:
    /// - Status 200: sempre retorna um objeto VtexHealthResponse
    /// - O campo "status" indica o resultado real (Connected, Unauthorized, Timeout, etc)
    /// 
    /// Resposta quando VTEX está desabilitado:
    /// - Status 200: retorna VtexHealthResponse com status=Disabled
    /// </summary>
    private static async Task<VtexHealthResponse> GetHealthCheckAsync(
        [FromServices] IVtexClient vtexClient,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

        var result = await vtexClient.CheckConnectionAsync(correlationId, cancellationToken);

        // Determinar se a integração está habilitada (para o response)
        var isEnabled = result.Status != VtexHealthStatus.Disabled;

        // Montar a resposta mascarando a BaseUrl se necessário
        var baseUrl = isEnabled ? MaskBaseUrlForResponse() : null;

        return new VtexHealthResponse
        {
            Enabled = isEnabled,
            Status = result.Status.ToString(),
            BaseUrl = baseUrl,
            ResponseTimeMs = result.ResponseTimeMs,
            TimestampUtc = DateTime.UtcNow.ToString("O"),
            Message = result.Message
        };
    }

    /// <summary>
    /// Mascara a URL base do VTEX para segurança na resposta.
    /// </summary>
    private static string MaskBaseUrlForResponse()
    {
        // Nota: Em um cenário real, você teria acesso a VtexOptions aqui
        // Para esta implementação, retornamos um placeholder mascarado
        // A URL real seria mascarada da mesma forma que em VtexClient.MaskBaseUrl()
        return "https://***/[store]";
    }
}

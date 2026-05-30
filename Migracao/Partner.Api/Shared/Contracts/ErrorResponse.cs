namespace Partner.Api.Shared.Contracts;

public sealed class ErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}

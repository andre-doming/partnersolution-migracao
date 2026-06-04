namespace Partner.Api.Infrastructure.Import;

public static class ImportJobStatus
{
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string CompletedWithErrors = "CompletedWithErrors";
    public const string Failed = "Failed";
    public const string CancellationRequested = "CancellationRequested";
    public const string Cancelled = "Cancelled";

    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        Completed,
        CompletedWithErrors,
        Failed,
        Cancelled
    };

    public static bool IsTerminal(string status)
    {
        return TerminalStatuses.Contains(status);
    }
}
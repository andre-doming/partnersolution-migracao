namespace Partner.Api.Infrastructure.Import;

public static class ImportJobStateMachine
{
    public static bool CanTransition(string currentStatus, string nextStatus)
    {
        if (string.Equals(currentStatus, ImportJobStatus.Queued, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(nextStatus, ImportJobStatus.Running, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nextStatus, ImportJobStatus.CancellationRequested, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nextStatus, ImportJobStatus.Cancelled, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(currentStatus, ImportJobStatus.Running, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(nextStatus, ImportJobStatus.Completed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nextStatus, ImportJobStatus.CompletedWithErrors, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nextStatus, ImportJobStatus.Failed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nextStatus, ImportJobStatus.CancellationRequested, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nextStatus, ImportJobStatus.Cancelled, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(currentStatus, ImportJobStatus.CancellationRequested, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(nextStatus, ImportJobStatus.Cancelled, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
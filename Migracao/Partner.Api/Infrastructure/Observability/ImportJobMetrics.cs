using System.Threading;

namespace Partner.Api.Infrastructure.Observability;

public static class ImportJobMetrics
{
    private static long _totalJobs;
    private static long _completedJobs;
    private static long _failedJobs;

    public static void MarkStarted() => Interlocked.Increment(ref _totalJobs);

    public static void MarkCompleted(bool failed)
    {
        if (failed)
        {
            Interlocked.Increment(ref _failedJobs);
            return;
        }

        Interlocked.Increment(ref _completedJobs);
    }

    public static object Snapshot() => new
    {
        totalJobs = Interlocked.Read(ref _totalJobs),
        completedJobs = Interlocked.Read(ref _completedJobs),
        failedJobs = Interlocked.Read(ref _failedJobs)
    };
}


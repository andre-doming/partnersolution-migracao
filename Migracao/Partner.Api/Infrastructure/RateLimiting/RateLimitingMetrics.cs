using System.Threading;

namespace Partner.Api.Infrastructure.RateLimiting;

public sealed class RateLimitingMetrics
{
    private long _authLoginRejected;
    private long _authMfaVerifyRejected;
    private long _authMfaActivateRejected;
    private long _authMfaSetupRejected;
    private long _adminMfaResetRejected;
    private long _adminMfaUnlockRejected;

    public void Increment(string policyName)
    {
        switch (policyName)
        {
            case RateLimitingPolicyNames.AuthLogin:
                Interlocked.Increment(ref _authLoginRejected);
                break;
            case RateLimitingPolicyNames.AuthMfaVerify:
                Interlocked.Increment(ref _authMfaVerifyRejected);
                break;
            case RateLimitingPolicyNames.AuthMfaActivate:
                Interlocked.Increment(ref _authMfaActivateRejected);
                break;
            case RateLimitingPolicyNames.AuthMfaSetup:
                Interlocked.Increment(ref _authMfaSetupRejected);
                break;
            case RateLimitingPolicyNames.AdminMfaReset:
                Interlocked.Increment(ref _adminMfaResetRejected);
                break;
            case RateLimitingPolicyNames.AdminMfaUnlock:
                Interlocked.Increment(ref _adminMfaUnlockRejected);
                break;
        }
    }

    public object Snapshot() => new
    {
        authLoginRejected = Interlocked.Read(ref _authLoginRejected),
        authMfaVerifyRejected = Interlocked.Read(ref _authMfaVerifyRejected),
        authMfaActivateRejected = Interlocked.Read(ref _authMfaActivateRejected),
        authMfaSetupRejected = Interlocked.Read(ref _authMfaSetupRejected),
        adminMfaResetRejected = Interlocked.Read(ref _adminMfaResetRejected),
        adminMfaUnlockRejected = Interlocked.Read(ref _adminMfaUnlockRejected)
    };
}
namespace Partner.Api.Features.Auth.Mfa;

public static class MfaRules
{
    public const string StatusLoginSuccess = "LOGIN_SUCCESS";
    public const string StatusMfaRequired = "MFA_REQUIRED";
    public const string StatusMfaSetupRequired = "MFA_SETUP_REQUIRED";

    public static string ResolveLoginStatus(UserMfaState state)
    {
        if (!state.MfaEnabled || state.MfaResetRequired)
        {
            return StatusMfaSetupRequired;
        }

        return StatusMfaRequired;
    }

    public static bool IsPendingExpired(MfaPendingSession pending, DateTime now)
    {
        return pending.ConsumedAt is not null || pending.ExpiresAt <= now;
    }

    public static bool TryGetLockoutMinutes(UserMfaState state, DateTime now, out int minutes)
    {
        minutes = 0;
        if (state.MfaLockoutUntil is null || state.MfaLockoutUntil <= now)
        {
            return false;
        }

        minutes = (int)Math.Ceiling((state.MfaLockoutUntil.Value - now).TotalMinutes);
        return true;
    }

    public static string BuildLockoutMessage(int minutes)
    {
        return $"Conta temporariamente bloqueada. Tente novamente após {minutes} minutos.";
    }
}
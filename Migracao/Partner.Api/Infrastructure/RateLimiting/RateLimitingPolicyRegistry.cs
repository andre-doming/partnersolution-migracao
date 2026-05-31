namespace Partner.Api.Infrastructure.RateLimiting;

public sealed class RateLimitingPolicyRegistry
{
    private readonly HashSet<string> _policies = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string policyName) => _policies.Add(policyName);

    public bool Contains(string policyName) => _policies.Contains(policyName);

    public IReadOnlyCollection<string> All => _policies.ToArray();
}
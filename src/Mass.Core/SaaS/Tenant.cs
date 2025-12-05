namespace Mass.Core.SaaS;

public class Tenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public TenantLimits Limits => Tier switch
    {
        SubscriptionTier.Free => new TenantLimits(3, 10, 100, false),
        SubscriptionTier.Pro => new TenantLimits(25, -1, 10000, true),
        SubscriptionTier.Enterprise => new TenantLimits(-1, -1, -1, true),
        _ => new TenantLimits(0, 0, 0, false)
    };
}

public enum SubscriptionTier
{
    Free,
    Pro,
    Enterprise
}

public record TenantLimits(
    int MaxAgents,
    int MaxWorkflows,
    int MaxExecutionsPerMonth,
    bool PrioritySupport
)
{
    public bool IsUnlimited(int value) => value == -1;
}

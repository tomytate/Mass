namespace Mass.Core.SaaS;

public class Subscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public decimal MonthlyPrice { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
}

public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled,
    Expired
}

public class UsageRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public UsageMetric Metric { get; set; }
    public int Quantity { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? ReferenceId { get; set; }
}

public enum UsageMetric
{
    WorkflowExecution,
    AgentHeartbeat,
    UsbBurn,
    PxeBoot,
    PluginInstall
}

public interface IUsageTracker
{
    void TrackUsage(string tenantId, UsageMetric metric, int quantity = 1, string? referenceId = null);
    UsageSummary GetUsageSummary(string tenantId, DateTime from, DateTime to);
    bool IsWithinLimits(string tenantId, UsageMetric metric);
}

public record UsageSummary(
    string TenantId,
    DateTime From,
    DateTime To,
    int WorkflowExecutions,
    int UsbBurns,
    int PxeBoots,
    int ActiveAgents
);

public class InMemoryUsageTracker : IUsageTracker
{
    private readonly List<UsageRecord> _records = new();
    private readonly ITenantProvider _tenantProvider;

    public InMemoryUsageTracker(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public void TrackUsage(string tenantId, UsageMetric metric, int quantity = 1, string? referenceId = null)
    {
        _records.Add(new UsageRecord
        {
            TenantId = tenantId,
            Metric = metric,
            Quantity = quantity,
            ReferenceId = referenceId
        });
    }

    public UsageSummary GetUsageSummary(string tenantId, DateTime from, DateTime to)
    {
        var records = _records.Where(r => r.TenantId == tenantId && r.RecordedAt >= from && r.RecordedAt <= to).ToList();
        
        return new UsageSummary(
            tenantId,
            from,
            to,
            records.Where(r => r.Metric == UsageMetric.WorkflowExecution).Sum(r => r.Quantity),
            records.Where(r => r.Metric == UsageMetric.UsbBurn).Sum(r => r.Quantity),
            records.Where(r => r.Metric == UsageMetric.PxeBoot).Sum(r => r.Quantity),
            records.Where(r => r.Metric == UsageMetric.AgentHeartbeat).Select(r => r.ReferenceId).Distinct().Count()
        );
    }

    public bool IsWithinLimits(string tenantId, UsageMetric metric)
    {
        var tenant = _tenantProvider.CurrentTenant;
        if (tenant == null) return false;

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var summary = GetUsageSummary(tenantId, startOfMonth, DateTime.UtcNow);

        return metric switch
        {
            UsageMetric.WorkflowExecution => tenant.Limits.IsUnlimited(tenant.Limits.MaxExecutionsPerMonth) || 
                                             summary.WorkflowExecutions < tenant.Limits.MaxExecutionsPerMonth,
            _ => true
        };
    }
}

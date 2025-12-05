namespace Mass.Core.Marketplace;

public class Plugin
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public PluginCategory Category { get; set; } = PluginCategory.Utility;
    public PluginPricing Pricing { get; set; } = new();
    public int DownloadCount { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsVerified { get; set; }
    public bool IsOfficial { get; set; }
    public string PackageUrl { get; set; } = string.Empty;
    public string[] SupportedPlatforms { get; set; } = new[] { "windows" };
    public string MinimumMassVersion { get; set; } = "1.0.0";
}

public enum PluginCategory
{
    Utility,
    Workflow,
    Integration,
    Security,
    Backup,
    Deployment,
    Theme
}

public class PluginPricing
{
    public PricingType Type { get; set; } = PricingType.Free;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int RevenueSharePercentage { get; set; } = 70; // Author gets 70%, platform gets 30%
}

public enum PricingType
{
    Free,
    OneTime,
    Subscription
}

public class PluginInstallation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PluginId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string InstalledVersion { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class PluginReview
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PluginId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public interface IPluginMarketplace
{
    Task<IEnumerable<Plugin>> SearchAsync(string? query = null, PluginCategory? category = null, int skip = 0, int take = 20);
    Task<Plugin?> GetByIdAsync(string id);
    Task<IEnumerable<Plugin>> GetTrendingAsync(int count = 10);
    Task<IEnumerable<Plugin>> GetByAuthorAsync(string authorId);
    Task InstallAsync(string pluginId, string tenantId);
    Task UninstallAsync(string pluginId, string tenantId);
    Task<IEnumerable<PluginInstallation>> GetInstalledAsync(string tenantId);
}

public class InMemoryPluginMarketplace : IPluginMarketplace
{
    private readonly List<Plugin> _plugins = new()
    {
        new Plugin
        {
            Id = "autowim",
            Name = "AutoWIM",
            Description = "Automatic WIM file processing and optimization",
            Version = "1.2.0",
            Author = "MassSuite",
            AuthorId = "official",
            Category = PluginCategory.Utility,
            DownloadCount = 2400,
            Rating = 4.8,
            RatingCount = 156,
            IsOfficial = true,
            IsVerified = true
        },
        new Plugin
        {
            Id = "cloudsync",
            Name = "CloudSync",
            Description = "Sync workflows and configurations to cloud storage",
            Version = "1.0.0",
            Author = "MassSuite",
            AuthorId = "official",
            Category = PluginCategory.Integration,
            DownloadCount = 1800,
            Rating = 4.5,
            RatingCount = 89,
            IsOfficial = true,
            IsVerified = true
        },
        new Plugin
        {
            Id = "secureboot",
            Name = "SecureBoot Helper",
            Description = "Simplify Secure Boot configuration and signing",
            Version = "2.0.1",
            Author = "Community",
            AuthorId = "user123",
            Category = PluginCategory.Security,
            DownloadCount = 956,
            Rating = 4.2,
            RatingCount = 45,
            IsVerified = true
        }
    };

    private readonly List<PluginInstallation> _installations = new();

    public Task<IEnumerable<Plugin>> SearchAsync(string? query = null, PluginCategory? category = null, int skip = 0, int take = 20)
    {
        var results = _plugins.AsEnumerable();
        
        if (!string.IsNullOrEmpty(query))
            results = results.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        
        if (category.HasValue)
            results = results.Where(p => p.Category == category);

        return Task.FromResult(results.Skip(skip).Take(take));
    }

    public Task<Plugin?> GetByIdAsync(string id) => 
        Task.FromResult(_plugins.FirstOrDefault(p => p.Id == id));

    public Task<IEnumerable<Plugin>> GetTrendingAsync(int count = 10) =>
        Task.FromResult(_plugins.OrderByDescending(p => p.DownloadCount).Take(count).AsEnumerable());

    public Task<IEnumerable<Plugin>> GetByAuthorAsync(string authorId) =>
        Task.FromResult(_plugins.Where(p => p.AuthorId == authorId).AsEnumerable());

    public Task InstallAsync(string pluginId, string tenantId)
    {
        var plugin = _plugins.FirstOrDefault(p => p.Id == pluginId);
        if (plugin != null)
        {
            plugin.DownloadCount++;
            _installations.Add(new PluginInstallation
            {
                PluginId = pluginId,
                TenantId = tenantId,
                InstalledVersion = plugin.Version
            });
        }
        return Task.CompletedTask;
    }

    public Task UninstallAsync(string pluginId, string tenantId)
    {
        var installation = _installations.FirstOrDefault(i => i.PluginId == pluginId && i.TenantId == tenantId);
        if (installation != null)
            _installations.Remove(installation);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<PluginInstallation>> GetInstalledAsync(string tenantId) =>
        Task.FromResult(_installations.Where(i => i.TenantId == tenantId).AsEnumerable());
}

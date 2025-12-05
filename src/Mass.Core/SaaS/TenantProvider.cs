namespace Mass.Core.SaaS;

public interface ITenantProvider
{
    string? CurrentTenantId { get; }
    Tenant? CurrentTenant { get; }
    void SetTenant(string tenantId);
}

public class TenantProvider : ITenantProvider
{
    private readonly AsyncLocal<string?> _currentTenantId = new();
    private readonly ITenantRepository _repository;

    public TenantProvider(ITenantRepository repository)
    {
        _repository = repository;
    }

    public string? CurrentTenantId => _currentTenantId.Value;
    
    public Tenant? CurrentTenant => CurrentTenantId != null 
        ? _repository.GetById(CurrentTenantId) 
        : null;

    public void SetTenant(string tenantId)
    {
        _currentTenantId.Value = tenantId;
    }
}

public interface ITenantRepository
{
    Tenant? GetById(string id);
    Tenant? GetBySlug(string slug);
    IEnumerable<Tenant> GetAll();
    void Add(Tenant tenant);
    void Update(Tenant tenant);
    void Delete(string id);
}

public class InMemoryTenantRepository : ITenantRepository
{
    private readonly Dictionary<string, Tenant> _tenants = new();

    public Tenant? GetById(string id) => 
        _tenants.TryGetValue(id, out var tenant) ? tenant : null;

    public Tenant? GetBySlug(string slug) => 
        _tenants.Values.FirstOrDefault(t => t.Slug == slug);

    public IEnumerable<Tenant> GetAll() => _tenants.Values;

    public void Add(Tenant tenant) => _tenants[tenant.Id] = tenant;
    
    public void Update(Tenant tenant) => _tenants[tenant.Id] = tenant;
    
    public void Delete(string id) => _tenants.Remove(id);
}

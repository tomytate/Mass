namespace Mass.Core.Configuration;

public interface IConfigurationManager
{
    UnifiedConfiguration Current { get; }
    Task LoadAsync();
    Task SaveAsync();
    void Update(Action<UnifiedConfiguration> updateAction);
}

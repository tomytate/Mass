namespace Mass.Core.Abstractions;

public interface IConfigurationService
{
    T Get<T>(string key, T defaultValue);
    void Set<T>(string key, T value);
    Task SaveAsync();
    Task LoadAsync();
    void Reset();
}

using System.Text.Json;
using Mass.Core.Abstractions;

namespace Mass.Core.Configuration;

public class JsonConfigurationService : IConfigurationService
{
    private readonly string _filePath;
    private readonly Dictionary<string, object> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isDirty;

    public JsonConfigurationService(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            if (value is JsonElement element)
            {
                return element.Deserialize<T>() ?? defaultValue;
            }
            return (T)value;
        }
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _cache[key] = value!;
        _isDirty = true;
    }

    public async Task SaveAsync()
    {
        if (!_isDirty) return;

        await _lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_filePath, json);
            _isDirty = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                _cache.Clear();
                return;
            }

            var json = await File.ReadAllTextAsync(_filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            if (data != null)
            {
                _cache.Clear();
                foreach (var kvp in data)
                {
                    _cache[kvp.Key] = kvp.Value;
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Reset()
    {
        _cache.Clear();
        _isDirty = true;
    }
}

using System.Collections.Generic;

namespace Mass.Core.Devices;

public class CachedDeviceDetector : IDeviceDetector, IDisposable
{
    private readonly IDeviceDetector _inner;
    private readonly TimeSpan _cacheDuration;
    private readonly OrderedDictionary<string, IStorageDevice> _cache = new();
    private readonly object _cacheLock = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _isMonitoring;

    public event EventHandler<IStorageDevice>? DeviceConnected;
    public event EventHandler<IStorageDevice>? DeviceDisconnected;

    public CachedDeviceDetector(IDeviceDetector inner, TimeSpan? cacheDuration = null)
    {
        _inner = inner;
        _cacheDuration = cacheDuration ?? TimeSpan.FromSeconds(5);
        
        _inner.DeviceConnected += OnInnerDeviceConnected;
        _inner.DeviceDisconnected += OnInnerDeviceDisconnected;
    }

    public async Task<IEnumerable<IStorageDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - _lastRefresh < _cacheDuration && _cache.Count > 0)
        {
            lock (_cacheLock)
            {
                return [.. _cache.Values];
            }
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (DateTime.UtcNow - _lastRefresh < _cacheDuration && _cache.Count > 0)
            {
                lock (_cacheLock)
                {
                    return [.._cache.Values];
                }
            }

            var devices = await _inner.GetDevicesAsync(cancellationToken);
            
            lock (_cacheLock)
            {
                _cache.Clear();
                foreach (var device in devices)
                {
                    _cache.TryAdd(device.Id, device);
                }
            }
            
            _lastRefresh = DateTime.UtcNow;
            
            lock (_cacheLock)
            {
                return [.. _cache.Values];
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;
        
        _inner.StartMonitoring();
        _isMonitoring = true;
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;
        
        _inner.StopMonitoring();
        _isMonitoring = false;
    }

    public void InvalidateCache()
    {
        _lastRefresh = DateTime.MinValue;
    }

    private void OnInnerDeviceConnected(object? sender, IStorageDevice device)
    {
        lock (_cacheLock)
        {
            _cache.TryAdd(device.Id, device);
        }
        DeviceConnected?.Invoke(this, device);
    }

    private void OnInnerDeviceDisconnected(object? sender, IStorageDevice device)
    {
        lock (_cacheLock)
        {
            _cache.Remove(device.Id);
        }
        DeviceDisconnected?.Invoke(this, device);
    }

    public void Dispose()
    {
        StopMonitoring();
        _inner.DeviceConnected -= OnInnerDeviceConnected;
        _inner.DeviceDisconnected -= OnInnerDeviceDisconnected;
        _refreshLock.Dispose();
        
        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Domain;

namespace ProUSB.Domain.Drivers {
    public interface IDiskDriver : IDisposable {
        string PhysicalId { get; }
        long Capacity { get; }
        int SectorSize { get; }
        DeviceBusType BusType { get; }
        Task ExclusiveLockAsync(CancellationToken ct);
        Task UnlockAsync(CancellationToken ct);
        Task WriteSectorsAsync(long offset, byte[] data, CancellationToken ct);
        Task<byte[]> ReadSectorsAsync(long offset, int count, CancellationToken ct);
        Task RefreshPartitionTableAsync(CancellationToken ct);
    }
    public interface IDriverFactory {
        Task<IDiskDriver> OpenDriverAsync(string deviceId, bool writeAccess, CancellationToken ct);
        Task<IEnumerable<UsbDeviceInfo>> EnumerateDevicesAsync(CancellationToken ct);
    }
}

namespace ProUSB.Domain.Services {
    public interface IBurnStrategy {
        BurnStrategy StrategyType { get; }
        Task ExecuteAsync(DeploymentConfiguration c, IProgress<WriteStatistics> p, CancellationToken ct);
        Task VerifyAsync(DeploymentConfiguration c, IProgress<WriteStatistics> p, CancellationToken ct);
    }
    public interface ISafetyGuard {
        DeviceRiskLevel EvaluateRisk(UsbDeviceInfo device);
    }
}


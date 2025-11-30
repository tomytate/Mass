using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Services.Logging;

namespace ProUSB.Infrastructure.DiskManagement.Vds;

public class VdsPartitionRefresher {
    private readonly FileLogger _log;

    public VdsPartitionRefresher(FileLogger log) {
        _log = log;
    }

    public async Task<bool> RefreshPartitionsAsync(CancellationToken ct) {
        return await Task.Run(() => RefreshPartitions(), ct);
    }

    private bool RefreshPartitions() {
        if (!OperatingSystem.IsWindows()) {
            _log.Debug("VDS is only available on Windows");
            return false;
        }

        IVdsServiceLoader? loader = null;
        IVdsService? service = null;

        try {
            _log.Info("Attempting VDS partition refresh...");
            
            var vdsLoaderType = Type.GetTypeFromCLSID(new Guid("9C38ED61-D565-4728-AEEE-C80952F0ECDE"));
            if (vdsLoaderType == null) {
                _log.Warn("VDS Loader type not found");
                return false;
            }
            
            var vdsLoaderObj = Activator.CreateInstance(vdsLoaderType);
            if (vdsLoaderObj == null) {
                _log.Warn("Failed to create VDS Loader instance");
                return false;
            }

            loader = vdsLoaderObj as IVdsServiceLoader;
            if (loader == null) {
                _log.Warn("VDS Loader instance is not of expected type");
                return false;
            }

            int hr = loader.LoadService(null!, out service);
            if (hr != 0 || service == null) {
                _log.Warn($"VDS LoadService failed: 0x{hr:X8}");
                return false;
            }

            hr = service.WaitForServiceReady();
            if (hr != 0) {
                _log.Warn($"VDS WaitForServiceReady failed: 0x{hr:X8}");
                return false;
            }

            _log.Info("VDS service ready, calling Reenumerate...");
            hr = service.Reenumerate();
            if (hr != 0) {
                _log.Warn($"VDS Reenumerate failed: 0x{hr:X8}");
                return false;
            }

            _log.Info("Calling VDS Refresh...");
            hr = service.Refresh();
            if (hr != 0) {
                _log.Warn($"VDS Refresh failed: 0x{hr:X8}");
                return false;
            }

            _log.Info("VDS partition refresh successful");
            return true;

        } catch (Exception ex) {
            _log.Warn($"VDS refresh exception: {ex.Message}");
            return false;
        } finally {
            if (OperatingSystem.IsWindows()) {
                if (service != null) {
                    try { Marshal.ReleaseComObject(service); } catch { }
                }
                if (loader != null) {
                    try {  Marshal.ReleaseComObject(loader); } catch { }
                }
            }
        }
    }
}



using System;
using System.Runtime.InteropServices;
using ProUSB.Services.Logging;

namespace ProUSB.Infrastructure.DiskManagement.Vds;

public class VdsRescanService {
    private readonly FileLogger _log;

    [Flags]
    public enum RescanType {
        Refresh = 1,      
        Reenumerate = 2   
    }

    public VdsRescanService(FileLogger log) {
        _log = log;
    }

    public async System.Threading.Tasks.Task<bool> RescanAsync(RescanType rescanType, int sleepAfterMs = 0) {
        _log.Info("Forcing VDS rescan to prevent Windows from 'losing' the disk...");
        
        try {
            var refresher = new VdsPartitionRefresher(_log);
            await refresher.RefreshPartitionsAsync(System.Threading.CancellationToken.None);
            
            if (sleepAfterMs > 0) {
                _log.Info($"Waiting {sleepAfterMs}ms for rescan to complete...");
                await System.Threading.Tasks.Task.Delay(sleepAfterMs);
            }
            
            _log.Info("VDS rescan completed successfully");
            return true;
        } catch (Exception ex) {
            _log.Warn($"VDS rescan failed: {ex.Message}");
            return false;
        }
    }
}




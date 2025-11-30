using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Domain.Services;
using ProUSB.Domain.Drivers;
using ProUSB.Infrastructure.DiskManagement;
using ProUSB.Services.Logging;

namespace ProUSB.Services.Burn;

public class ParallelBurnService {
    private readonly IEnumerable<IBurnStrategy> _s;
    private readonly ISafetyGuard _g;
    private readonly IDriverFactory _f;
    private readonly DriveSafetyValidator _safetyValidator;
    private readonly FileLogger _log;
    
    public ParallelBurnService(IEnumerable<IBurnStrategy> s, ISafetyGuard g, IDriverFactory f, FileLogger log) {
        _s = s;
        _g = g;
        _f = f;
        _log = log;
        _safetyValidator = new DriveSafetyValidator(log);
    }
    
    public async Task BurnAsync(DeploymentConfiguration c, IProgress<WriteStatistics> p, CancellationToken ct) {
        var list = await _f.EnumerateDevicesAsync(ct);
        var fresh = list.FirstOrDefault(x => x.DeviceId == c.TargetDevice.DeviceId) 
            ?? throw new Exception("Missing Device");

        var safetyViolations = _safetyValidator.ValidateDrive(fresh, c.SourceIso.FilePath);
        if (safetyViolations != DriveSafetyValidator.SafetyViolation.None) {
            string message = _safetyValidator.GetViolationMessage(safetyViolations);
            _log.Error(message);
            
            if (_safetyValidator.IsCritical(safetyViolations)) {
                throw new InvalidOperationException($"SAFETY VIOLATION: Cannot proceed.\n{message}");
            }
        }

        var r = _g.EvaluateRisk(fresh);
        if (r == DeviceRiskLevel.Critical || r == DeviceRiskLevel.SystemLockdown) 
            throw new Exception($"Risk Block: {r}");
        
        var e = _s.FirstOrDefault(x => x.StrategyType == c.Strategy) 
            ?? throw new Exception("Missing Engine");
        
        await e.ExecuteAsync(c, p, ct);
        await e.VerifyAsync(c, p, ct);
    }
}


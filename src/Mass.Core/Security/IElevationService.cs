using System.Security.Principal;

namespace Mass.Core.Security;

public interface IElevationService
{
    bool IsElevated { get; }
    bool RequiresElevation(string operation);
    Task<bool> RequestElevationAsync(string reason);
    void RestartAsAdmin();
}

using System.Threading;
using System.Threading.Tasks;

namespace Mass.Core.Services;

public interface IIpcService
{
    Task<bool> StartServerAsync(CancellationToken ct = default);
    Task<bool> StopServerAsync(CancellationToken ct = default);
    Task<string> GetStatusAsync(CancellationToken ct = default);
}

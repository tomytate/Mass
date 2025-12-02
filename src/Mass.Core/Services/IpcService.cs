using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mass.Core.Services;

public class IpcService : IIpcService
{
    private const string PipeName = "MassBootServerPipe";
    private const string ServerExecutable = "ProPXEServer.API.exe"; // Adjust based on actual output
    private Process? _serverProcess;

    public async Task<bool> StartServerAsync(CancellationToken ct = default)
    {
        try
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
                return true;

            string serverPath = FindServerExecutable();
            if (string.IsNullOrEmpty(serverPath))
                return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _serverProcess = Process.Start(startInfo);
            return _serverProcess != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StopServerAsync(CancellationToken ct = default)
    {
        try
        {
            if (_serverProcess == null || _serverProcess.HasExited)
                return true;

            _serverProcess.Kill();
            await _serverProcess.WaitForExitAsync(ct);
            _serverProcess = null;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetStatusAsync(CancellationToken ct = default)
    {
        if (_serverProcess == null || _serverProcess.HasExited)
            return "Stopped";

        return "Running";
    }

    private string FindServerExecutable()
    {
        string current = AppDomain.CurrentDomain.BaseDirectory;
        string devPath = Path.GetFullPath(Path.Combine(current, "../../../../src/ProPXEServer/ProPXEServer.API/bin/Debug/net10.0/ProPXEServer.API.exe"));
        
        if (File.Exists(devPath)) return devPath;
        
        string prodPath = Path.Combine(current, "ProPXEServer.API.exe");
        if (File.Exists(prodPath)) return prodPath;

        return string.Empty;
    }
}

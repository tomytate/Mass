using Mass.Core.Abstractions;
using Mass.Spec.Contracts.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProPXEServer.Plugin;

public class ProPXEServerPlugin : IPlugin
{
    private System.Diagnostics.Process? _serverProcess;

    public PluginManifest Manifest => new()
    {
        Id = "ProPXEServer",
        Name = "ProPXEServer",
        Version = "1.0.0",
        Description = "PXE network boot server",
        Author = "Tomy Tolledo",
        Icon = "🖥️",
        EntryAssembly = "ProPXEServer.Plugin.dll",
        EntryType = "ProPXEServer.Plugin.ProPXEServerPlugin",
        Enabled = true,
        Permissions = new List<string> { "network", "filesystem" }
    };

    public void Init(IServiceProvider services)
    {
        // Initialize services if needed
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_serverProcess == null)
        {
            // Locate the executable
            // Assuming standard build output structure relative to the plugin dll
            var pluginDir = AppContext.BaseDirectory;
            // We need to find ProPXEServer.API.exe. 
            // In dev, it might be in a different path, but for now let's try to find it relative to the plugin or known path.
            
            // For development environment, we know the path relative to the solution
            var apiPath = Path.GetFullPath(Path.Combine(pluginDir, "..", "..", "..", "..", "ProPXEServer", "ProPXEServer.API", "bin", "Debug", "net10.0", "ProPXEServer.API.exe"));
            
            if (!File.Exists(apiPath))
            {
                // Try production/published path (usually in the same folder or a subfolder)
                apiPath = Path.Combine(pluginDir, "ProPXEServer.API.exe");
            }

            if (File.Exists(apiPath))
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = apiPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(apiPath)
                };

                _serverProcess = System.Diagnostics.Process.Start(startInfo);
            }
        }
        
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try 
            {
                _serverProcess.Kill();
                await _serverProcess.WaitForExitAsync(cancellationToken);
            }
            catch { /* Ignore errors during shutdown */ }
            finally
            {
                _serverProcess.Dispose();
                _serverProcess = null;
            }
        }
    }
}


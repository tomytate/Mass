using System.IO;
using System.Threading.Tasks;
using System.Threading;
using ProUSB.Services.Logging;
namespace ProUSB.Services.Burn.Strategies;
public class Windows11BypassInjector {
    private readonly FileLogger _log;
    
    public Windows11BypassInjector(FileLogger log) {
        _log = log;
    }
    
    public async Task InjectAsync(string r, CancellationToken ct) {
        _log.Info($"Injecting Windows 11 bypass to: {r}");
        string x = @"<?xml version=""1.0""?><unattend xmlns=""urn:schemas-microsoft-com:unattend"" xmlns:wcm=""http://schemas.microsoft.com/WmiConfig/2002/State""><settings pass=""windowsPE""><component name=""Microsoft-Windows-Setup"" processorArchitecture=""amd64"" publicKeyToken=""31bf3856ad364e35"" language=""neutral"" versionScope=""nonSxS""><RunSynchronous><RunSynchronousCommand wcm:action=""add""><Order>1</Order><Path>reg add HKLM\SYSTEM\Setup\LabConfig /v BypassTPMCheck /t REG_DWORD /d 1 /f</Path></RunSynchronousCommand><RunSynchronousCommand wcm:action=""add""><Order>2</Order><Path>reg add HKLM\SYSTEM\Setup\LabConfig /v BypassSecureBootCheck /t REG_DWORD /d 1 /f</Path></RunSynchronousCommand><RunSynchronousCommand wcm:action=""add""><Order>3</Order><Path>reg add HKLM\SYSTEM\Setup\LabConfig /v BypassRAMCheck /t REG_DWORD /d 1 /f</Path></RunSynchronousCommand></RunSynchronous></component></settings></unattend>";
        var path = Path.Combine(r, "autounattend.xml");
        await File.WriteAllTextAsync(path, x, ct);
        _log.Info($"Created autounattend.xml at: {path}");
    }
}



using System;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Domain.Drivers;
using ProUSB.Domain.Services;
using ProUSB.Infrastructure.DiskManagement;
using ProUSB.Services.Iso;
using ProUSB.Services.Logging;
using DiscUtils;

namespace ProUSB.Services.Burn.Strategies;

public class FileSystemDeploymentStrategy : IBurnStrategy {
    public BurnStrategy StrategyType => BurnStrategy.FileSystemCopy;
    private readonly IDriverFactory _f;
    private readonly IsoIntegrityVerifier _v;
    private readonly NativeDiskFormatter _dp;
    private readonly FileLogger _log;
    
    public FileSystemDeploymentStrategy(IDriverFactory f, IsoIntegrityVerifier v, NativeDiskFormatter d, FileLogger log) { 
        _f=f;_v=v;_dp=d;_log=log; 
    }

    private string? _mountedDriveLetter;

    public async Task ExecuteAsync(DeploymentConfiguration c, IProgress<WriteStatistics> p, CancellationToken ct) {
        _mountedDriveLetter = null;
        _log.Info("=== FILE SYSTEM DEPLOYMENT START ===");
        ct.ThrowIfCancellationRequested();
        _log.Info($"ISO: {c.SourceIso.FilePath}");
        _log.Info($"Device: {c.TargetDevice.FriendlyName} ({c.TargetDevice.DeviceId})");
        _log.Info($"Partition Scheme: {c.PartitionScheme}, FileSystem: {c.FileSystem}");
        
        _log.Info("Validating ISO file...");
        if(!File.Exists(c.SourceIso.FilePath)) {
            _log.Error($"ISO file not found: {c.SourceIso.FilePath}");
            throw new FileNotFoundException($"ISO file not found: {c.SourceIso.FilePath}");
        }
        
        var isoInfo = new FileInfo(c.SourceIso.FilePath);
        _log.Info($"ISO size: {isoInfo.Length} bytes ({isoInfo.Length/1024.0/1024.0:F2} MB)");
        
        if(!await _v.IsStructureValid(c.SourceIso.FilePath)) {
            _log.Error("ISO file failed validation check");
            throw new Exception("Invalid or corrupted ISO file");
        }
        _log.Info("ISO validation passed");
        
        _log.Info("Starting format operation...");
        p.Report(new WriteStatistics{Message="Formatting"});
        
        if (c.CustomLayout != null && c.CustomLayout.Any()) {
            _log.Info("Custom partition layout detected. Using FormatCustomAsync...");
            await _dp.FormatCustomAsync(c.TargetDevice.PhysicalIndex, c.CustomLayout, ct);
        } else {
            await _dp.FormatAsync(c.TargetDevice.PhysicalIndex, c.VolumeLabel, c.FileSystem, c.PartitionScheme, c.ClusterSize, c.QuickFormat, ct);
        }
        
        ct.ThrowIfCancellationRequested();
        _log.Info("Format completed successfully");
        
        p.Report(new WriteStatistics{Message="Mounting"});
        _log.Info("Mounting volume using advanced multi-API technique...");
        
        var advancedMounter = new ProUSB.Infrastructure.DiskManagement.AdvancedVolumeMounter(_log);
        
        long expectedOffset = DiskHelper.GetExpectedOffset(c.PartitionScheme);

        string? let = null;
        try {
            let = await advancedMounter.WaitForVolumeAsync(
                c.TargetDevice.PhysicalIndex, 
                expectedOffset,
                60,
                ct,
                wildcard: false);
            
            _log.Info($"Volume mounted successfully: {let}");
        } catch (MountFailedException ex) {
            _log.Error($"Advanced volume mounting timeout: {ex.Message}");
            _log.Info("Fallback: Trying legacy enumeration method...");
            
            await Task.Delay(2000, ct);
            var ls=await _f.EnumerateDevicesAsync(ct);
            var d=ls.FirstOrDefault(x=>x.DeviceId==c.TargetDevice.DeviceId);
            if(d?.MountPoints.Any()==true) { 
                let=d.MountPoints[0]; 
                _log.Info($"Legacy enumeration found volume: {let}");
            } else {
                throw new MountFailedException(
                    "USB drive formatted successfully but failed to mount after 60s using both " +
                    "advanced and legacy methods. Please manually assign a drive letter " +
                    "in Disk Management (diskmgmt.msc) or replug the USB drive.", ex);
            }
        }
        _mountedDriveLetter = let;

        p.Report(new WriteStatistics{Message="Extracting"});
        _log.Info("Opening ISO for extraction...");
        
        using var iso = File.OpenRead(c.SourceIso.FilePath);
        DiscUtils.DiscFileSystem r = DiscUtils.Udf.UdfReader.Detect(iso) ? new DiscUtils.Udf.UdfReader(iso) : new DiscUtils.Iso9660.CDReader(iso, true);
        _log.Info($"ISO filesystem type: {(DiscUtils.Udf.UdfReader.Detect(iso) ? "UDF" : "ISO9660")}");
        
        byte[] buf = new byte[4*1024*1024];
        
        _log.Info("Calculating total size...");
        long tot = isoInfo.Length;
        _log.Info($"Total ISO size: {tot} bytes ({tot/1024.0/1024.0:F2} MB)");
        
        long cur=0;
        int copiedFiles = 0;
        
        foreach(var f in r.GetFiles(r.Root.FullName,"*.*",SearchOption.AllDirectories)) {
            ct.ThrowIfCancellationRequested();
            var inf = r.GetFileInfo(f);
            string dst = Path.Combine(let+"\\", f.TrimStart('\\'));
            
            _log.Debug($"Copying: {inf.Name} ({inf.Length} bytes) -> {dst}");
            
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                
                bool isFat32 = c.FileSystem.Equals("FAT32", StringComparison.OrdinalIgnoreCase);
                if (isFat32 && inf.Length >= 4294967295) {
                    if (inf.Name.EndsWith(".wim", StringComparison.OrdinalIgnoreCase)) {
                        _log.Info($"Large WIM file detected ({inf.Name}) on FAT32. Initiating split operation...");
                        await SplitAndCopyWimAsync(inf, dst, p, ct);
                    } else {
                        throw new IOException($"File {inf.Name} is too large ({inf.Length} bytes) for FAT32 destination and is not a split-able WIM file.");
                    }
                } else {
                    using var si = inf.OpenRead(); 
                    using var so = File.Create(dst);
                    
                    int k; 
                    while((k=await si.ReadAsync(buf,0,buf.Length,ct))>0) {
                        await so.WriteAsync(buf,0,k,ct);
                        cur+=k;
                        double pct = Math.Min(99, (double)cur/tot*100);
                        p.Report(new WriteStatistics{PercentComplete=pct, Message=$"Copying {inf.Name}"});
                    }
                }
                copiedFiles++;
            }
            catch(Exception ex) {
                _log.Error($"Failed to copy file: {inf.Name}", ex);
                throw;
            }
        }
        _log.Info($"File extraction complete. Copied {copiedFiles} files");

        if (c.PersistenceSize > 0) {
            try {
                await CreatePersistenceFileAsync(let, c.PersistenceSize, p, ct);
            } catch (Exception ex) {
                _log.Error($"Failed to create persistence file: {ex.Message}");

            }
        }

        try {
            var patcher = new ProUSB.Domain.Services.IsoPatcher(_log);
            await patcher.PatchAsync(let, ct);
        } catch (Exception ex) {
            _log.Warn($"Linux ISO patching failed (non-fatal): {ex.Message}");
        }
        
        if(c.BypassWin11) { 
            _log.Info("Applying Windows 11 bypass...");
            p.Report(new WriteStatistics{Message="Patching"}); 
            await new Windows11BypassInjector(_log).InjectAsync(let, ct); 
            _log.Info("Windows 11 bypass applied");
        }

        p.Report(new WriteStatistics{Message="Success", PercentComplete=100});
        _log.Info("=== FILE SYSTEM DEPLOYMENT SUCCESS ===");
    }

    private async Task SplitAndCopyWimAsync(DiscFileInfo inf, string dst, IProgress<WriteStatistics> p, CancellationToken ct) {
        string tempFile = Path.GetTempFileName();
        try {
            _log.Info($"Extracting {inf.Name} to temporary location for splitting...");
            p.Report(new WriteStatistics{Message=$"Extracting {inf.Name} for splitting..."});
            
            using (var si = inf.OpenRead())
            using (var so = File.Create(tempFile)) {
                await si.CopyToAsync(so, ct);
            }
            
            string destSwm = Path.ChangeExtension(dst, ".swm");
            _log.Info($"Splitting WIM to {destSwm}...");
            p.Report(new WriteStatistics{Message="Splitting WIM file..."});
            
            var psi = new System.Diagnostics.ProcessStartInfo {
                FileName = "dism.exe",
                Arguments = $"/Split-Image /ImageFile:\"{tempFile}\" /SWMFile:\"{destSwm}\" /FileSize:3800",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) throw new Exception("Failed to start DISM for splitting");
            
            await proc.WaitForExitAsync(ct);
            
            if (proc.ExitCode != 0) {
                string err = await proc.StandardError.ReadToEndAsync(ct);
                string outStr = await proc.StandardOutput.ReadToEndAsync(ct);
                throw new Exception($"DISM splitting failed: {err}\n{outStr}");
            }
            
            _log.Info("WIM splitting completed successfully");
        } finally {
            if (File.Exists(tempFile)) {
                File.Delete(tempFile);
            }
        }
    }

    public async Task VerifyAsync(DeploymentConfiguration c, IProgress<WriteStatistics> p, CancellationToken ct) {
        _log.Info("=== VERIFICATION PHASE START ===");
        p.Report(new WriteStatistics{Message="Verifying files...", PercentComplete=0});
        
        string? let = _mountedDriveLetter;
        if (string.IsNullOrEmpty(let)) {
            _log.Warn("Cached drive letter missing, attempting to enumerate...");
            var ls = await _f.EnumerateDevicesAsync(ct);
            var d = ls.FirstOrDefault(x => x.DeviceId == c.TargetDevice.DeviceId);
            if (d?.MountPoints.Any() == true) {
                let = d.MountPoints[0];
            } else {
                throw new Exception("Drive not mounted for verification");
            }
        }

        var verificationResult = await VerifyDeployment(let!, c.SourceIso.FilePath, ct);
        
        if (!verificationResult.Success) {
            _log.Error($"Verification failed: {verificationResult.ErrorMessage}");
            throw new Exception($"Verification failed: {verificationResult.ErrorMessage}");
        }
        
        _log.Info($"Verification passed: {verificationResult.FilesVerified} files OK");
        p.Report(new WriteStatistics{Message="Verified", PercentComplete=100});
    }

    private async Task<VerificationResult> VerifyDeployment(string usbRoot, string isoPath, CancellationToken ct)
    {
        try {
            var copiedFiles = GetFilesSafe(usbRoot);
            _log.Info($"Verification: Found {copiedFiles.Count} files on USB");
            
            if (copiedFiles.Count == 0) {
                return new VerificationResult { 
                    Success = false, 
                    ErrorMessage = "No files found on USB drive" 
                };
            }
            
            bool hasBootFiles = copiedFiles.Any(f => 
                Path.GetFileName(f).Contains("boot", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(f).Contains("efi", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".efi", StringComparison.OrdinalIgnoreCase));
            
            if (!hasBootFiles) {
                _log.Warn("No boot files detected - USB may not be bootable");
            }
            
            using var isoStream = File.OpenRead(isoPath);
            var reader = DiscUtils.Udf.UdfReader.Detect(isoStream) 
                ? (DiscUtils.DiscFileSystem)new DiscUtils.Udf.UdfReader(isoStream) 
                : new DiscUtils.Iso9660.CDReader(isoStream, true);
            
            var criticalFiles = new[] { "bootmgr", "bootmgr.efi", "boot.wim", "install.wim", "install.esd" };
            foreach (var critical in criticalFiles) {
                if (reader.FileExists($"\\{critical}") || reader.FileExists($"\\sources\\{critical}")) {
                    string expectedPath = Path.Combine(usbRoot, critical);
                    string sourcesPath = Path.Combine(usbRoot, "sources", critical);
                    
                    if (!File.Exists(expectedPath) && !File.Exists(sourcesPath)) {
                        _log.Warn($"Critical file missing: {critical}");
                    }
                }
            }
            
            long totalSize = copiedFiles.Sum(f => new FileInfo(f).Length);
            _log.Info($"Total copied size: {totalSize / 1024.0 / 1024.0:F2} MB");

            
            string md5Path = Path.Combine(usbRoot, "md5sum.txt");
            if (File.Exists(md5Path)) {
                _log.Info("Found md5sum.txt, performing integrity check...");
                int verifiedCount = await VerifyMd5Sums(md5Path, usbRoot, ct);
                _log.Info($"MD5 integrity check passed for {verifiedCount} files.");
            }
            
            return new VerificationResult { 
                Success = true, 
                FilesVerified = copiedFiles.Count,
                TotalSize = totalSize,
                HasBootFiles = hasBootFiles
            };
        }
        catch (Exception ex) {
            _log.Error("Verification exception", ex);
            return new VerificationResult { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    private async Task<int> VerifyMd5Sums(string md5Path, string rootDir, CancellationToken ct) {
        var lines = await File.ReadAllLinesAsync(md5Path, ct);
        int count = 0;
        using var md5 = MD5.Create();
        
        foreach (var line in lines) {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            
            var parts = line.Split(new[] { ' ', '*' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            
            string expectedHash = parts[0];
            string filename = string.Join(" ", parts.Skip(1)).TrimStart('.', '/', '\\').Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(rootDir, filename);
            
            if (!File.Exists(fullPath)) {
                _log.Warn($"Missing file listed in md5sum.txt: {filename}");
                continue;
            }
            
            using var stream = File.OpenRead(fullPath);
            var hashBytes = await md5.ComputeHashAsync(stream, ct);
            var actualHash = Convert.ToHexString(hashBytes);
            
            if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase)) {
                throw new Exception($"MD5 mismatch for {filename}. Expected {expectedHash}, got {actualHash}");
            }
            count++;
        }
        return count;
    }

    private List<string> GetFilesSafe(string root) {
        var files = new List<string>();
        var dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0) {
            string currentDir = dirs.Pop();
            try {
                foreach (var file in Directory.GetFiles(currentDir)) {
                    files.Add(file);
                }
                foreach (var dir in Directory.GetDirectories(currentDir)) {
                    var name = Path.GetFileName(dir);
                    if (name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                    dirs.Push(dir);
                }
            } catch (UnauthorizedAccessException) {
                _log.Warn($"Skipping inaccessible directory: {currentDir}");
            } catch (Exception ex) {
                _log.Warn($"Error scanning directory {currentDir}: {ex.Message}");
            }
        }
        return files;
    }

    private async Task CreatePersistenceFileAsync(string driveLetter, int sizeMb, IProgress<WriteStatistics> p, CancellationToken ct) {
        _log.Info($"Creating persistence file ({sizeMb} MB)...");
        p.Report(new WriteStatistics{Message="Creating persistence..."});
        
        string path = Path.Combine(driveLetter, "casper-rw");
        long sizeBytes = (long)sizeMb * 1024 * 1024;

        using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
            fs.SetLength(sizeBytes);

            try {
                _log.Info("Attempting to format using WSL (mke2fs)...");
                string wslPath = ConvertToWslPath(path);
                
                var psi = new System.Diagnostics.ProcessStartInfo {
                    FileName = "wsl",
                    Arguments = $"mke2fs -t ext3 -F \"{wslPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) throw new Exception("Failed to start WSL");
                
                await proc.WaitForExitAsync(ct);
                
                if (proc.ExitCode != 0) {
                    string err = await proc.StandardError.ReadToEndAsync(ct);
                    _log.Warn($"WSL format failed (Exit {proc.ExitCode}): {err}. Persistence file created but NOT formatted.");
                } else {
                    _log.Info("Persistence file successfully formatted as Ext3 via WSL.");
                }
            } catch (Exception ex) {
                _log.Warn($"WSL format failed: {ex.Message}. Persistence file created but NOT formatted.");
            }
        }
    }

    private string ConvertToWslPath(string winPath) {

        if (string.IsNullOrEmpty(winPath) || winPath.Length < 2 || winPath[1] != ':') return winPath;
        char drive = char.ToLowerInvariant(winPath[0]);
        string path = winPath.Substring(2).Replace('\\', '/');
        return $"/mnt/{drive}{path}";
    }

    private record VerificationResult {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = "";
        public int FilesVerified { get; init; }
        public long TotalSize { get; init; }
        public bool HasBootFiles { get; init; }
    }
}




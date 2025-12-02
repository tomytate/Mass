using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Services.Logging;

namespace ProUSB.Domain.Services;

public class IsoPatcher {
    private readonly FileLogger _log;

    private static readonly HashSet<string> SyslinuxCfgs = new(StringComparer.OrdinalIgnoreCase) {
        "isolinux.cfg", "syslinux.cfg", "extlinux.conf", "txt.cfg", "live.cfg"
    };
    
    private static readonly HashSet<string> GrubCfgs = new(StringComparer.OrdinalIgnoreCase) {
        "grub.cfg", "loopback.cfg"
    };

    private const string MenuCfg = "menu.cfg";

    public IsoPatcher(FileLogger log) {
        _log = log;
    }

    public async Task PatchAsync(string driveLetter, CancellationToken ct) {
        _log.Info("=== LINUX ISO PATCHING START ===");
        
        var configFiles = new List<string>();
        FindConfigFiles(driveLetter, configFiles);

        if (configFiles.Count == 0) {
            _log.Info("No Linux bootloader config files found to patch.");
            return;
        }

        _log.Info($"Found {configFiles.Count} config files to potential patch.");

        foreach (var file in configFiles) {
            ct.ThrowIfCancellationRequested();
            await PatchConfigFileAsync(file, ct);
        }
        
        _log.Info("=== LINUX ISO PATCHING COMPLETE ===");
    }

    private void FindConfigFiles(string rootPath, List<string> results) {
        try {
            foreach (var file in Directory.GetFiles(rootPath)) {
                var name = Path.GetFileName(file);
                if (IsConfigFile(name)) {
                    results.Add(file);
                }
            }

            foreach (var dir in Directory.GetDirectories(rootPath)) {
                var dirName = Path.GetFileName(dir);

                if (dirName.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                FindConfigFiles(dir, results);
            }
        } catch (Exception ex) {
            _log.Warn($"Error scanning directory {rootPath}: {ex.Message}");
        }
    }

    private bool IsConfigFile(string filename) {
        return SyslinuxCfgs.Contains(filename) || 
               GrubCfgs.Contains(filename) || 
               filename.Equals(MenuCfg, StringComparison.OrdinalIgnoreCase) ||
               (filename.EndsWith(".conf", StringComparison.OrdinalIgnoreCase) && filename.Length > 5); 
    }

    private async Task PatchConfigFileAsync(string filePath, CancellationToken ct) {
        try {
            string content = await File.ReadAllTextAsync(filePath, ct);
            string originalContent = content;
            bool modified = false;
            string filename = Path.GetFileName(filePath);
            
            bool isGrub = GrubCfgs.Contains(filename);
            bool isSyslinux = SyslinuxCfgs.Contains(filename);
            bool isMenu = filename.Equals(MenuCfg, StringComparison.OrdinalIgnoreCase);

            if (isGrub || isMenu || isSyslinux) {

                if (Regex.IsMatch(content, @"file=/cdrom/preseed")) {
                     content = Regex.Replace(content, @"(file=/cdrom/preseed)", "persistent $1");
                     if (content != originalContent) {
                         _log.Info($"Patched {filename}: Added 'persistent' (preseed match)");
                         modified = true;
                     }

                     if (isGrub && content.Contains("maybe-ubiquity")) {
                         content = content.Replace("maybe-ubiquity", "");
                         _log.Info($"Patched {filename}: Removed 'maybe-ubiquity'");
                         modified = true;
                     }
                }

                else if (content.Contains("boot=casper")) {
                    if (!content.Contains("boot=casper persistent")) {
                        content = content.Replace("boot=casper", "boot=casper persistent");
                        _log.Info($"Patched {filename}: Added 'persistent' (boot=casper match)");
                        modified = true;
                    }
                }

                else if (content.Contains("/casper/vmlinuz")) {
                    if (!content.Contains("/casper/vmlinuz persistent")) {
                        content = content.Replace("/casper/vmlinuz", "/casper/vmlinuz persistent");
                        _log.Info($"Patched {filename}: Added 'persistent' (vmlinuz match)");
                        modified = true;
                    }
                }

                else if (content.Contains("boot=live")) {
                    if (!content.Contains("boot=live persistence")) {
                        content = content.Replace("boot=live", "boot=live persistence");
                        _log.Info($"Patched {filename}: Added 'persistence' (boot=live match)");
                        modified = true;
                    }
                }
            }

            if (content.Contains("inst.stage2=") && !filePath.Contains("netinst", StringComparison.OrdinalIgnoreCase)) {
                 content = content.Replace("inst.stage2=", "inst.repo=");
                 _log.Info($"Patched {filename}: Replaced inst.stage2= with inst.repo=");
                 modified = true;
            }

            if (modified) {

                var attr = File.GetAttributes(filePath);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
                    File.SetAttributes(filePath, attr & ~FileAttributes.ReadOnly);
                }
                
                // Atomic write: Write to temp file, then move/replace
                string tempFile = filePath + ".tmp";
                await File.WriteAllTextAsync(tempFile, content, ct);
                
                try {
                    File.Move(tempFile, filePath, overwrite: true);
                    _log.Info($"Saved patched config file (atomic): {filePath}");
                } catch (Exception ex) {
                    _log.Error($"Atomic move failed for {filePath}: {ex.Message}");
                    // Fallback to direct write if move fails (unlikely but safe)
                    await File.WriteAllTextAsync(filePath, content, ct);
                    File.Delete(tempFile);
                }
            }

        } catch (Exception ex) {
            _log.Warn($"Failed to patch {filePath}: {ex.Message}");
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Services.Logging;

namespace ProUSB.Services.IsoDownload;

public class IsoDownloadService : IDisposable {
    private readonly FileLogger _logger;
    private readonly HttpClient _httpClient;

    public IsoDownloadService(FileLogger logger) {
        _logger = logger;
        _httpClient = new HttpClient {
            Timeout = Timeout.InfiniteTimeSpan
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ProUSBMediaSuite/1.0");
    }

    public async Task<bool> DownloadIsoAsync(
        OsInfo os,
        string outputPath,
        IProgress<DownloadProgress> progress,
        CancellationToken ct
    ) {
        try {
            if (string.IsNullOrEmpty(os.DirectDownloadUrl)) {
                _logger.Info($"No direct download available for {os.Name}, opening download page");
                OpenDownloadPage(os.DownloadPageUrl!);
                return false;
            }

            _logger.Info($"Starting download: {os.Name} to {outputPath}");

            using var response = await _httpClient.GetAsync(
                os.DirectDownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            );

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? os.FileSizeBytes ?? 0;
            var buffer = new byte[81920]; 
            long bytesDownloaded = 0;
            var stopwatch = Stopwatch.StartNew();
            var lastUpdate = DateTime.UtcNow;

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(outputPath);

            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0) {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                bytesDownloaded += bytesRead;

                
                if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds >= 200) {
                    var speedBps = bytesDownloaded / stopwatch.Elapsed.TotalSeconds;
                    var remaining = totalBytes > 0 && speedBps > 0
                        ? TimeSpan.FromSeconds((totalBytes - bytesDownloaded) / speedBps)
                        : TimeSpan.Zero;

                    progress.Report(new DownloadProgress {
                        PercentComplete = totalBytes > 0 ? (double)bytesDownloaded / totalBytes * 100 : 0,
                        BytesDownloaded = bytesDownloaded,
                        TotalBytes = totalBytes,
                        SpeedMBps = speedBps / 1_048_576,
                        TimeRemaining = remaining,
                        Status = $"Downloading... {FormatBytes(bytesDownloaded)} / {FormatBytes(totalBytes)}"
                    });

                    lastUpdate = DateTime.UtcNow;
                }
            }

            _logger.Info($"Download complete: {outputPath} ({FormatBytes(bytesDownloaded)})");

            
            if (!string.IsNullOrEmpty(os.Sha256Checksum)) {
                progress.Report(new DownloadProgress {
                    PercentComplete = 100,
                    BytesDownloaded = bytesDownloaded,
                    TotalBytes = totalBytes,
                    SpeedMBps = 0,
                    TimeRemaining = TimeSpan.Zero,
                    Status = "Verifying checksum..."
                });

                var isValid = await VerifyChecksumAsync(outputPath, os.Sha256Checksum, ct);
                if (!isValid) {
                    _logger.Error($"Checksum verification failed for {outputPath}");
                    File.Delete(outputPath);
                    throw new Exception("Checksum verification failed. Downloaded file may be corrupted.");
                }

                _logger.Info($"Checksum verification passed for {outputPath}");
            }

            progress.Report(new DownloadProgress {
                PercentComplete = 100,
                BytesDownloaded = bytesDownloaded,
                TotalBytes = totalBytes,
                SpeedMBps = 0,
                TimeRemaining = TimeSpan.Zero,
                Status = "Complete"
            });

            return true;

        } catch (OperationCanceledException) {
            _logger.Info($"Download cancelled: {os.Name}");
            if (File.Exists(outputPath)) {
                File.Delete(outputPath);
            }
            throw;
        } catch (Exception ex) {
            _logger.Error($"Download failed for {os.Name}: {ex.Message}", ex);
            if (File.Exists(outputPath)) {
                File.Delete(outputPath);
            }
            throw;
        }
    }

    public void OpenDownloadPage(string url) {
        try {
            Process.Start(new ProcessStartInfo {
                FileName = url,
                UseShellExecute = true
            });
            _logger.Info($"Opened download page: {url}");
        } catch (Exception ex) {
            _logger.Error($"Failed to open download page: {ex.Message}", ex);
        }
    }

    private async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256, CancellationToken ct) {
        try {
            await using var stream = File.OpenRead(filePath);
            var hashBytes = await SHA256.HashDataAsync(stream, ct);
            var actualHash = Convert.ToHexString(hashBytes);
            
            return actualHash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase);
        } catch (Exception ex) {
            _logger.Error($"Checksum verification error: {ex.Message}", ex);
            return false;
        }
    }

    private string FormatBytes(long bytes) {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public void Dispose() {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}




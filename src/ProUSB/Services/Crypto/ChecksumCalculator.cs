using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ProUSB.Services.Logging;

namespace ProUSB.Services.Crypto;

public enum HashAlgorithmType {
    MD5,
    SHA1,
    SHA256
}

public record ChecksumResult {
    public string Hash { get; init; } = "";
    public HashAlgorithmType Algorithm { get; init; }
    public long BytesProcessed { get; init; }
    public TimeSpan Duration { get; init; }
}

public class ChecksumCalculator {
    private readonly FileLogger _log;

    public ChecksumCalculator(FileLogger log) {
        _log = log;
    }

    public async Task<ChecksumResult> CalculateAsync(
        string filePath, 
        HashAlgorithmType algorithm,
        IProgress<double>? progress,
        CancellationToken ct) {

        _log.Info($"Calculating {algorithm} checksum for: {filePath}");
        var startTime = DateTime.UtcNow;

        try {
            using var stream = File.OpenRead(filePath);
            using var hashAlgorithm = CreateHashAlgorithm(algorithm);

            long totalBytes = stream.Length;
            long bytesRead = 0;
            byte[] buffer = new byte[8192];
            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0) {
                hashAlgorithm.TransformBlock(buffer, 0, read, null, 0);
                bytesRead += read;

                progress?.Report((double)bytesRead / totalBytes * 100);
            }

            hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            
            string hash = Convert.ToHexString(hashAlgorithm.Hash!).ToLowerInvariant();

            var duration = DateTime.UtcNow - startTime;
            _log.Info($"{algorithm} checksum: {hash} (calculated in {duration.TotalSeconds:F2}s)");

            return new ChecksumResult {
                Hash = hash,
                Algorithm = algorithm,
                BytesProcessed = bytesRead,
                Duration = duration
            };

        } catch (Exception ex) {
            _log.Error($"Checksum calculation failed: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<bool> VerifyAsync(
        string filePath,
        string expectedHash,
        HashAlgorithmType algorithm,
        CancellationToken ct) {

        var result = await CalculateAsync(filePath, algorithm, null, ct);
        bool matches = string.Equals(result.Hash, expectedHash, StringComparison.OrdinalIgnoreCase);

        if (matches) {
            _log.Info("Checksum verification PASSED");
        } else {
            _log.Warn($"Checksum verification FAILED - Expected: {expectedHash}, Got: {result.Hash}");
        }

        return matches;
    }

    private HashAlgorithm CreateHashAlgorithm(HashAlgorithmType type) {
        return type switch {
            HashAlgorithmType.MD5 => MD5.Create(),
            HashAlgorithmType.SHA1 => SHA1.Create(),
            HashAlgorithmType.SHA256 => SHA256.Create(),
            _ => throw new ArgumentException($"Unsupported algorithm: {type}")
        };
    }
}



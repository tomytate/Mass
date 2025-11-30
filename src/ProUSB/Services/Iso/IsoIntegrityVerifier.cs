using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;
using System;

namespace ProUSB.Services.Iso;
public class IsoIntegrityVerifier {
    public async Task<bool> IsStructureValid(string p) {
        try {
            using var f = File.OpenRead(p);
            if(f.Length<65536) return false;
            byte[] b=new byte[2048]; f.Seek(32768, SeekOrigin.Begin);
            await f.ReadExactlyAsync(b);
            
            bool isIso = b[1]=='C' && b[2]=='D' && b[3]=='0' && b[4]=='0' && b[5]=='1';
            if(isIso) return true;
            
            
            
            bool isUdf = b[1]=='B' && b[2]=='E' && b[3]=='A' && b[4]=='0' && b[5]=='1';
            return isUdf;
        } catch { return false; }
    }

    public async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256, CancellationToken ct) {
        try {
            using var stream = File.OpenRead(filePath);
            var hashBytes = await System.Security.Cryptography.SHA256.HashDataAsync(stream, ct);
            var actualHash = System.Convert.ToHexString(hashBytes);
            return actualHash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase);
        } catch { return false; }
    }
}


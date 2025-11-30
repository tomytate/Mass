using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using ProUSB.Domain;
using ProUSB.Domain.Drivers;
using ProUSB.Domain.Services;
namespace ProUSB.Services.Burn.Strategies;
public class RawPipelinedWriteStrategy : IBurnStrategy {
    public BurnStrategy StrategyType => BurnStrategy.RawSectorWrite;
    private readonly IDriverFactory _f;
    private const int CH = 4*1024*1024;
    public RawPipelinedWriteStrategy(IDriverFactory f)=>_f=f;
    public async Task ExecuteAsync(DeploymentConfiguration c, IProgress<WriteStatistics> p, CancellationToken ct) {
        using var d = await _f.OpenDriverAsync(c.TargetDevice.DeviceId, true, ct);
        if(new FileInfo(c.SourceIso.FilePath).Length > d.Capacity) throw new Exception("ISO > Device");
        await d.ExclusiveLockAsync(ct);
        try {
            var ch = Channel.CreateBounded<(byte[], int)>(3); var pl = Channel.CreateBounded<byte[]>(3);
            for(int i=0;i<3;i++) await pl.Writer.WriteAsync(new byte[CH], ct);
            long w=0, tot=new FileInfo(c.SourceIso.FilePath).Length;
            var sw=System.Diagnostics.Stopwatch.StartNew();
            var pr = Task.Run(async()=>{
                using var fs=new FileStream(c.SourceIso.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
                try{
                    while(true){
                        var b=await pl.Reader.ReadAsync(ct); int r=await fs.ReadAsync(b,0,CH,ct);
                        if(r==0)break;
                        if(r%d.SectorSize!=0){ int al=((r+d.SectorSize-1)/d.SectorSize)*d.SectorSize; Array.Clear(b,r,al-r); r=al;}
                        await ch.Writer.WriteAsync((b,r),ct);
                    }
                } finally { ch.Writer.Complete(); }
            }, ct);
            while(await ch.Reader.WaitToReadAsync(ct)) {
                while(ch.Reader.TryRead(out var it)) {
                    await d.WriteSectorsAsync(w, it.Item1.AsSpan(0,it.Item2).ToArray(), ct);
                    w+=it.Item2; await pl.Writer.WriteAsync(it.Item1, ct);
                    if(sw.ElapsedMilliseconds>500) p.Report(new WriteStatistics{BytesWritten=w,PercentComplete=(double)w/tot*100,Message="Flash"});
                }
            }
            await pr;
            p.Report(new WriteStatistics{BytesWritten=w, PercentComplete=100, Message="Flash"});
        } finally { await d.UnlockAsync(ct); }
        p.Report(new WriteStatistics{Message="Done", PercentComplete=100});
    }
    public async Task VerifyAsync(DeploymentConfiguration c, IProgress<WriteStatistics> p, CancellationToken ct) {
        p.Report(new WriteStatistics{Message="Verifying...", PercentComplete=0});
        using var d = await _f.OpenDriverAsync(c.TargetDevice.DeviceId, false, ct);
        long len = new FileInfo(c.SourceIso.FilePath).Length;
        using var fs = new FileStream(c.SourceIso.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
        
        byte[] isoBuf = new byte[4 * 1024 * 1024];
        long offset = 0;
        
        while (offset < len) {
            ct.ThrowIfCancellationRequested();
            int toRead = (int)Math.Min(isoBuf.Length, len - offset);
            int isoRead = await fs.ReadAsync(isoBuf, 0, toRead, ct);
            
            if (isoRead == 0) break;
            
            byte[] diskBuf = await d.ReadSectorsAsync(offset, isoRead, ct);
            
            if (!isoBuf.AsSpan(0, isoRead).SequenceEqual(diskBuf.AsSpan(0, isoRead))) {
                throw new Exception($"Verification failed at offset {offset}. Data mismatch.");
            }
            
            offset += isoRead;
            p.Report(new WriteStatistics{Message="Verifying...", PercentComplete=(double)offset/len*100});
        }
        p.Report(new WriteStatistics{Message="Verified", PercentComplete=100});
    }
}


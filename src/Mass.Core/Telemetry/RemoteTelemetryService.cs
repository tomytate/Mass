using Mass.Spec.Contracts.Logging;
using System.Threading.Channels;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mass.Core.Telemetry;

public class RemoteTelemetryService : ITelemetryService, IDisposable, IAsyncDisposable
{
    private readonly Channel<TelemetryEvent> _eventChannel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;
    private readonly string _logPath;
    private bool _consentGiven;

    public bool ConsentGiven
    {
        get => _consentGiven;
        set => _consentGiven = value;
    }

    public RemoteTelemetryService()
    {
        _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Mass", "Logs");
        Directory.CreateDirectory(_logPath);

        // Unbounded channel for high throughput buffering
        _eventChannel = Channel.CreateUnbounded<TelemetryEvent>();
        _cts = new CancellationTokenSource();
        _processingTask = Task.Run(ProcessEventsAsync);
    }

    public void TrackEvent(TelemetryEvent e)
    {
        if (!ConsentGiven) return;
        _eventChannel.Writer.TryWrite(e);
    }

    public async Task FlushAsync()
    {
        // For local logging, we just ensure the writer is up to date? 
        // Real flushing is hard with unbounded channels without a signal, 
        // but we can just wait a tick for the processor to catch up.
        await Task.Delay(50);
    }

    private async Task ProcessEventsAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var filePath = Path.Combine(_logPath, $"telemetry-{today}.log");

                if (!await _eventChannel.Reader.WaitToReadAsync(_cts.Token)) break;

                using var writer = new StreamWriter(filePath, append: true) { AutoFlush = true };

                while (_eventChannel.Reader.TryRead(out var telemetryEvent))
                {
                    var json = JsonSerializer.Serialize(telemetryEvent);
                    await writer.WriteLineAsync($"[{DateTime.UtcNow:O}] {json}");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Fallback: wait before retrying to avoid tight loop on file access errors
                await Task.Delay(1000);
            }
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException) { }
        finally
        {
            _cts.Dispose();
        }
    }
}

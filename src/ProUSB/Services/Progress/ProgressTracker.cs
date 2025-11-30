using System;
using System.Diagnostics;
using ProUSB.Domain;

namespace ProUSB.Services.Progress;

public class ProgressTracker {
    private readonly Stopwatch _stopwatch = new();
    private readonly RollingAverage _speedCalc = new(10);
    private long _totalBytes;
    private long _lastBytes;
    private DateTime _lastUpdate = DateTime.UtcNow;

    public void Start(long totalBytes) {
        _totalBytes = totalBytes;
        _lastBytes = 0;
        _stopwatch.Restart();
        _lastUpdate = DateTime.UtcNow;
    }

    public WriteStatistics Update(long currentBytes, string message) {
        var now = DateTime.UtcNow;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        var deltaBytes = currentBytes - _lastBytes;

        double speedMBps = 0;
        if (deltaTime > 0.1) {
            speedMBps = (deltaBytes / deltaTime) / (1024.0 * 1024.0);
            _speedCalc.AddSample(speedMBps);
            _lastUpdate = now;
            _lastBytes = currentBytes;
        }

        var avgSpeed = _speedCalc.GetAverage();
        var percentComplete = _totalBytes > 0 ? (currentBytes * 100.0 / _totalBytes) : 0;
        
        TimeSpan timeRemaining = TimeSpan.Zero;
        if (avgSpeed > 0 && currentBytes < _totalBytes) {
            var remainingBytes = _totalBytes - currentBytes;
            var secondsRemaining = (remainingBytes / (1024.0 * 1024.0)) / avgSpeed;
            timeRemaining = TimeSpan.FromSeconds(secondsRemaining);
        }

        return new WriteStatistics {
            BytesWritten = currentBytes,
            PercentComplete = percentComplete,
            Message = message,
            SpeedMBps = avgSpeed,
            TimeElapsed = _stopwatch.Elapsed,
            TimeRemaining = timeRemaining
        };
    }

    public void Stop() {
        _stopwatch.Stop();
    }
}

public class RollingAverage {
    private readonly double[] _samples;
    private int _index;
    private int _count;

    public RollingAverage(int windowSize) {
        _samples = new double[windowSize];
    }

    public void AddSample(double value) {
        _samples[_index] = value;
        _index = (_index + 1) % _samples.Length;
        if (_count < _samples.Length) _count++;
    }

    public double GetAverage() {
        if (_count == 0) return 0;
        
        double sum = 0;
        for (int i = 0; i < _count; i++) {
            sum += _samples[i];
        }
        return sum / _count;
    }
}



using System.Collections.Concurrent;
using System.Text.Json;
using Mass.Spec.Contracts.Workflow;

namespace Mass.Agent;

/// <summary>
/// Local workflow queue for pending jobs with file-based persistence.
/// </summary>
public class WorkflowQueue
{
    private readonly ConcurrentQueue<QueuedWorkflow> _queue = new();
    private readonly string _persistencePath;

    public WorkflowQueue()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MassSuite", "Agent");
        Directory.CreateDirectory(appData);
        _persistencePath = Path.Combine(appData, "pending_workflows.json");
        LoadFromDisk();
    }

    public int Count => _queue.Count;

    public void Enqueue(WorkflowDefinition workflow)
    {
        _queue.Enqueue(new QueuedWorkflow
        {
            Workflow = workflow,
            QueuedAt = DateTime.UtcNow
        });
        SaveToDisk();
    }

    public QueuedWorkflow? Dequeue()
    {
        if (_queue.TryDequeue(out var item))
        {
            SaveToDisk();
            return item;
        }
        return null;
    }

    public QueuedWorkflow? Peek() =>
        _queue.TryPeek(out var item) ? item : null;

    private void LoadFromDisk()
    {
        try
        {
            if (File.Exists(_persistencePath))
            {
                var json = File.ReadAllText(_persistencePath);
                var items = JsonSerializer.Deserialize<List<QueuedWorkflow>>(json) ?? [];
                foreach (var item in items)
                {
                    _queue.Enqueue(item);
                }
            }
        }
        catch
        {
            // If load fails, start with empty queue
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var items = _queue.ToArray();
            var json = JsonSerializer.Serialize(items);
            File.WriteAllText(_persistencePath, json);
        }
        catch
        {
            // Best-effort persistence
        }
    }
}

public class QueuedWorkflow
{
    public WorkflowDefinition Workflow { get; set; } = new();
    public DateTime QueuedAt { get; set; }
    public int RetryCount { get; set; }
}

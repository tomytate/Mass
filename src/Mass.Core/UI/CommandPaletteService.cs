namespace Mass.Core.UI;

public class CommandPaletteService : ICommandPaletteService
{
    private readonly Dictionary<string, CommandPaletteItem> _commands = new();
    private readonly Queue<string> _recentCommands = new();
    private const int MaxRecentCommands = 10;

    public void RegisterCommand(string id, string title, string description, string category, Action action, string icon = "", string shortcut = "")
    {
        _commands[id] = new CommandPaletteItem
        {
            Id = id,
            Title = title,
            Description = description,
            Category = category,
            Action = action,
            Icon = icon,
            Shortcut = shortcut
        };
    }

    public void UnregisterCommand(string id)
    {
        _commands.Remove(id);
    }

    public IEnumerable<CommandPaletteItem> SearchCommands(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllCommands();

        var lowerQuery = query.ToLowerInvariant();
        
        return _commands.Values
            .Where(cmd => 
                cmd.Title.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                cmd.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                cmd.Category.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(cmd => 
                cmd.Title.StartsWith(lowerQuery, StringComparison.OrdinalIgnoreCase) ? 2 :
                cmd.Title.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
    }

    public IEnumerable<CommandPaletteItem> GetAllCommands()
    {
        return _commands.Values.OrderBy(c => c.Category).ThenBy(c => c.Title);
    }

    public void ExecuteCommand(string id)
    {
        if (_commands.TryGetValue(id, out var command))
        {
            command.Action?.Invoke();
            AddToRecentCommands(id);
        }
    }

    public IEnumerable<string> GetRecentCommands(int count = 5)
    {
        return _recentCommands.Reverse().Take(count);
    }

    public void AddToRecentCommands(string commandId)
    {
        if (_recentCommands.Contains(commandId))
        {
            var temp = _recentCommands.Where(c => c != commandId).ToList();
            _recentCommands.Clear();
            foreach (var cmd in temp)
                _recentCommands.Enqueue(cmd);
        }

        _recentCommands.Enqueue(commandId);
        
        while (_recentCommands.Count > MaxRecentCommands)
        {
            _recentCommands.Dequeue();
        }
    }
}

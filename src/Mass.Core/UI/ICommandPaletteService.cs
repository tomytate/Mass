namespace Mass.Core.UI;

public class CommandPaletteItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Shortcut { get; set; } = string.Empty;
    public Action? Action { get; set; }
}

public interface ICommandPaletteService
{
    void RegisterCommand(string id, string title, string description, string category, Action action, string icon = "", string shortcut = "");
    void UnregisterCommand(string id);
    IEnumerable<CommandPaletteItem> SearchCommands(string query);
    IEnumerable<CommandPaletteItem> GetAllCommands();
    void ExecuteCommand(string id);
    IEnumerable<string> GetRecentCommands(int count = 5);
    void AddToRecentCommands(string commandId);
}

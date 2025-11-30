namespace Mass.Core.Configuration;

public class UserPreferences
{
    public List<string> RecentFiles { get; set; } = new();
    public List<string> FavoriteModules { get; set; } = new();
    public WindowState? LastWindowState { get; set; }
}

public class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 900;
    public int Height { get; set; } = 700;
    public bool IsMaximized { get; set; }
}

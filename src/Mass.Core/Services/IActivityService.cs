namespace Mass.Core.Services;

public interface IActivityService
{
    IEnumerable<ActivityItem> GetRecentActivities();
    void AddActivity(string title, string description, string icon = "📝");
    void ClearHistory();

    IEnumerable<FavoriteItem> GetFavorites();
    void AddFavorite(string id, string type, string name, string icon, string target);
    void RemoveFavorite(string id);
    bool IsFavorite(string id);
}

public class ActivityItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "📝";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    public string TimeDisplay => Timestamp.ToString("HH:mm");
}

public class FavoriteItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Module, Workflow, Plugin
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "⭐";
    public string Target { get; set; } = string.Empty;
}

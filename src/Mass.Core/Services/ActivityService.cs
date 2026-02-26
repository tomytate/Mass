using System.Text.Json;
using Mass.Core.Interfaces;

namespace Mass.Core.Services;

public class ActivityService : IActivityService
{
    private readonly string _activityPath;
    private readonly string _favoritesPath;
    private readonly ILogService _logger;
    private List<ActivityItem> _activities = new();
    private List<FavoriteItem> _favorites = new();
    private const int MaxHistory = 50;

    public ActivityService(ILogService logger, string? storagePath = null)
    {
        _logger = logger;
        var basePath = storagePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite");
        Directory.CreateDirectory(basePath);
        _activityPath = Path.Combine(basePath, "activity_history.json");
        _favoritesPath = Path.Combine(basePath, "favorites.json");
        LoadData();
    }

    public IEnumerable<ActivityItem> GetRecentActivities()
    {
        return _activities.OrderByDescending(a => a.Timestamp).Take(10);
    }

    public void AddActivity(string title, string description, string icon = "📝")
    {
        _activities.Insert(0, new ActivityItem
        {
            Title = title,
            Description = description,
            Icon = icon,
            Timestamp = DateTime.Now
        });

        if (_activities.Count > MaxHistory)
        {
            _activities = _activities.Take(MaxHistory).ToList();
        }

        SaveActivities();
    }

    public void ClearHistory()
    {
        _activities.Clear();
        SaveActivities();
    }

    public IEnumerable<FavoriteItem> GetFavorites()
    {
        return _favorites;
    }

    public void AddFavorite(string id, string type, string name, string icon, string target)
    {
        if (!_favorites.Any(f => f.Id == id))
        {
            _favorites.Add(new FavoriteItem
            {
                Id = id,
                Type = type,
                Name = name,
                Icon = icon,
                Target = target
            });
            SaveFavorites();
        }
    }

    public void RemoveFavorite(string id)
    {
        var item = _favorites.FirstOrDefault(f => f.Id == id);
        if (item != null)
        {
            _favorites.Remove(item);
            SaveFavorites();
        }
    }

    public bool IsFavorite(string id)
    {
        return _favorites.Any(f => f.Id == id);
    }

    private void LoadData()
    {
        try
        {
            if (File.Exists(_activityPath))
            {
                var json = File.ReadAllText(_activityPath);
                _activities = JsonSerializer.Deserialize<List<ActivityItem>>(json) ?? new List<ActivityItem>();
            }

            if (File.Exists(_favoritesPath))
            {
                var json = File.ReadAllText(_favoritesPath);
                _favorites = JsonSerializer.Deserialize<List<FavoriteItem>>(json) ?? new List<FavoriteItem>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load activity data", ex, "ActivityService");
            // Ignore load errors, start fresh
            _activities = new List<ActivityItem>();
            _favorites = new List<FavoriteItem>();
        }
    }

    private void SaveActivities()
    {
        try
        {
            var json = JsonSerializer.Serialize(_activities);
            File.WriteAllText(_activityPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save activities", ex, "ActivityService");
        }
    }

    private void SaveFavorites()
    {
        try
        {
            var json = JsonSerializer.Serialize(_favorites);
            File.WriteAllText(_favoritesPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save favorites", ex, "ActivityService");
        }
    }
}

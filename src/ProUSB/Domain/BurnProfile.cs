using System;

namespace ProUSB.Domain;

public record BurnProfile {
    private string _name = "";
    public required string Name {
        get => _name;
        init {
            if(string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Profile name cannot be empty");
            }
            _name = value;
        }
    }
    
    public string Description { get; init; } = "";
    
    private string _partitionScheme = "gpt";
    public string PartitionScheme {
        get => _partitionScheme;
        init => _partitionScheme = value?.ToLowerInvariant() ?? "gpt";
    }
    
    private string _fileSystem = "fat32";
    public string FileSystem {
        get => _fileSystem;
        init => _fileSystem = value?.ToLowerInvariant() ?? "fat32";
    }
    
    private int _clusterSize;
    public int ClusterSize {
        get => _clusterSize;
        init => _clusterSize = Math.Max(0, value);
    }
    
    public bool QuickFormat { get; init; } = true;
    public bool BypassWin11 { get; init; } = false;
    public bool IsRaw { get; init; } = false;
    
    private int _persistenceSize;
    public int PersistenceSize {
        get => _persistenceSize;
        init => _persistenceSize = Math.Max(0, value);
    }
    
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime LastUsedAt { get; init; } = DateTime.Now;
    public bool IsDefault { get; init; } = false;
}


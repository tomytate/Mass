namespace Mass.Core.Domain.Models;

public sealed class Image : Entity<Guid>
{
    public string Name { get; private set; }
    public string FilePath { get; private set; }
    public ImageType Type { get; private set; }
    public long SizeBytes { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private Image(Guid id, string name, string filePath, ImageType type, long sizeBytes) : base(id)
    {
        Name = name;
        FilePath = filePath;
        Type = type;
        SizeBytes = sizeBytes;
        IsActive = true;
    }

    public static Image Create(string name, string filePath, ImageType type, long sizeBytes, string? description = null)
    {
        var image = new Image(Guid.NewGuid(), name, filePath, type, sizeBytes)
        {
            Description = description
        };
        image.RaiseDomainEvent(new ImageRegisteredEvent(image.Id, name, type));
        return image;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ImageActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ImageDeactivatedEvent(Id));
    }
}

public enum ImageType
{
    Iso,
    Img,
    Wim,
    Vhd,
    Vmdk
}

public sealed class ImageRegisteredEvent : DomainEvent
{
    public Guid ImageId { get; }
    public string Name { get; }
    public ImageType Type { get; }

    public ImageRegisteredEvent(Guid imageId, string name, ImageType type)
    {
        ImageId = imageId;
        Name = name;
        Type = type;
    }
}

public sealed class ImageActivatedEvent : DomainEvent
{
    public Guid ImageId { get; }

    public ImageActivatedEvent(Guid imageId)
    {
        ImageId = imageId;
    }
}

public sealed class ImageDeactivatedEvent : DomainEvent
{
    public Guid ImageId { get; }

    public ImageDeactivatedEvent(Guid imageId)
    {
        ImageId = imageId;
    }
}

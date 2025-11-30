namespace ProPXEServer.API.Data;

public class BootFile {
    public int Id { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}



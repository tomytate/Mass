using System.ComponentModel.DataAnnotations;

namespace ProPXEServer.API.Data;

public class BootConfiguration {
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(17)]
    public string MacAddress { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? CustomBootFile { get; set; }

    [MaxLength(50)]
    public string Architecture { get; set; } = "BIOS";

    public bool Enabled { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastBootedAt { get; set; }

    public int BootCount { get; set; } = 0;
}



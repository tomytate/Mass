using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProPXEServer.API.Data;

namespace ProPXEServer.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BootFilesController : ControllerBase {
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BootFilesController> _logger;
    private readonly string _pxeRoot;

    public BootFilesController(ApplicationDbContext db, ILogger<BootFilesController> logger, IConfiguration config) {
        _db = db;
        _logger = logger;
        _pxeRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pxe");
    }

    [HttpGet]
    public async Task<IActionResult> GetBootFiles() {
        try {
            var files = new List<object>();
            
            if (Directory.Exists(_pxeRoot)) {
                var allFiles = Directory.GetFiles(_pxeRoot, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in allFiles) {
                    var fileInfo = new FileInfo(file);
                    var relativePath = Path.GetRelativePath(_pxeRoot, file);
                    
                    files.Add(new {
                        id = files.Count + 1,
                        fileName = Path.GetFileName(file),
                        filePath = relativePath.Replace('\\', '/'),
                        fileSizeBytes = fileInfo.Length,
                        uploadedAt = fileInfo.CreationTimeUtc,
                        architecture = GetArchitecture(file)
                    });
                }
            }

            return Ok(files);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error listing boot files");
            return StatusCode(500, "Error listing boot files");
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadBootFile(IFormFile file) {
        if (file == null || file.Length == 0) {
            return BadRequest("No file provided");
        }

        try {
            var fileName = Path.GetFileName(file.FileName);
            var architecture = GetArchitectureFromFileName(fileName);
            var targetDir = Path.Combine(_pxeRoot, architecture.ToLower());
            
            Directory.CreateDirectory(targetDir);
            
            var filePath = Path.Combine(targetDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Uploaded boot file: {FileName} to {Architecture}", fileName, architecture);

            return Ok(new { 
                message = "File uploaded successfully",
                fileName,
                architecture,
                size = file.Length
            });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error uploading boot file");
            return StatusCode(500, "Error uploading boot file");
        }
    }

    [HttpGet("{id}/download")]
    public IActionResult DownloadBootFile(int id, [FromQuery] string? path) {
        try {
            if (string.IsNullOrEmpty(path)) {
                return BadRequest("File path required");
            }

            var filePath = Path.Combine(_pxeRoot, path);
            
            if (!System.IO.File.Exists(filePath)) {
                return NotFound("File not found");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var fileName = Path.GetFileName(filePath);
            
            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error downloading boot file");
            return StatusCode(500, "Error downloading boot file");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBootFile(int id, [FromQuery] string? path) {
        try {
            if (string.IsNullOrEmpty(path)) {
                return BadRequest("File path required");
            }

            var filePath = Path.Combine(_pxeRoot, path);
            
            if (!System.IO.File.Exists(filePath)) {
                return NotFound("File not found");
            }

            System.IO.File.Delete(filePath);
            _logger.LogInformation("Deleted boot file: {Path}", path);

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error deleting boot file");
            return StatusCode(500, "Error deleting boot file");
        }
    }

    private string GetArchitecture(string filePath) {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath)?.Split(Path.DirectorySeparatorChar).LastOrDefault()?.ToLowerInvariant();

        if (directory == "arm64" || fileName.Contains("arm64")) return "ARM64";
        if (directory == "uefi" || fileName.EndsWith(".efi")) return "UEFI";
        if (directory == "bios" || fileName.EndsWith(".kpxe") || fileName.EndsWith(".pxe")) return "BIOS";
        
        return "Unknown";
    }

    private string GetArchitectureFromFileName(string fileName) {
        var lower = fileName.ToLowerInvariant();
        if (lower.Contains("arm64")) return "ARM64";
        if (lower.EndsWith(".efi")) return "UEFI";
        if (lower.EndsWith(".kpxe") || lower.EndsWith(".pxe")) return "BIOS";
        return "BIOS";
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProPXEServer.API.Data;
using System.Text.RegularExpressions;

using Asp.Versioning;

namespace ProPXEServer.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public partial class BootFilesController : ControllerBase {
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BootFilesController> _logger;
    private readonly string _pxeRoot;

    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled)]
    private static partial Regex SafeFilenameRegex();
    
    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.\/]+$", RegexOptions.Compiled)]
    private static partial Regex SafePathRegex();

    public BootFilesController(ApplicationDbContext db, ILogger<BootFilesController> logger, IConfiguration config) {
        _db = db;
        _logger = logger;
        _pxeRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "www root", "pxe"));
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
            return StatusCode(500, new { message = "Error listing boot files" });
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(500_000_000)]
    public async Task<IActionResult> UploadBootFile(IFormFile file) {
        if (file == null || file.Length == 0) {
            return BadRequest(new { message = "No file provided" });
        }

        try {
            var fileName = Path.GetFileName(file.FileName);
            
            if (!SafeFilenameRegex().IsMatch(fileName)) {
                _logger.LogWarning("Rejected upload with unsafe filename: {FileName}", fileName);
                return BadRequest(new { message = "Invalid filename. Use only alphanumeric, dash, underscore, and dot." });
            }
            
            var architecture = GetArchitectureFromFileName(fileName);
            var targetDir = Path.Combine(_pxeRoot, architecture.ToLower());
            
            Directory.CreateDirectory(targetDir);
            
            var filePath = Path.Combine(targetDir, fileName);

            if (!Path.GetFullPath(filePath).StartsWith(_pxeRoot, StringComparison.OrdinalIgnoreCase)) {
                _logger.LogWarning("Path traversal attempt in upload: {FileName}", fileName);
                return BadRequest(new { message = "Invalid file path" });
            }

            await using (var stream = new FileStream(filePath, FileMode.Create)) {
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
            return StatusCode(500, new { message = "Error uploading boot file" });
        }
    }

    [HttpGet("{id}/download")]
    public IActionResult DownloadBootFile(int id, [FromQuery] string? path) {
        try {
            if (string.IsNullOrEmpty(path)) {
                return BadRequest(new { message = "File path required" });
            }

            if (path.AsSpan().Contains("..", StringComparison.Ordinal)) {
                _logger.LogWarning("Directory traversal attempt blocked: {Path}", path);
                return BadRequest(new { message = "Invalid path" });
            }

            if (!SafePathRegex().IsMatch(path.AsSpan())) {
                _logger.LogWarning("Blocked path with invalid characters: {Path}", path);
                return BadRequest(new { message = "Invalid path characters" });
            }

            var fullPath = Path.GetFullPath(Path.Combine(_pxeRoot, path));
            
            if (!fullPath.StartsWith(_pxeRoot, StringComparison.OrdinalIgnoreCase)) {
                _logger.LogWarning("Directory traversal attempt (normalized): {Path} -> {FullPath}", path, fullPath);
                return Forbid();
            }
            
            if (!System.IO.File.Exists(fullPath)) {
                return NotFound(new { message = "File not found" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            var fileName = Path.GetFileName(fullPath);
            
            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error downloading boot file");
            return StatusCode(500, new { message = "Error downloading boot file" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBootFile(int id, [FromQuery] string? path) {
        try {
            if (string.IsNullOrEmpty(path)) {
                return BadRequest(new { message = "File path required" });
            }

            if (path.Contains("..") || !SafePathRegex().IsMatch(path)) {
                _logger.LogWarning("Invalid delete path: {Path}", path);
                return BadRequest(new { message = "Invalid path" });
            }

            var filePath = Path.GetFullPath(Path.Combine(_pxeRoot, path));
            
            if (!filePath.StartsWith(_pxeRoot, StringComparison.OrdinalIgnoreCase)) {
                return Forbid();
            }
            
            if (!System.IO.File.Exists(filePath)) {
                return NotFound(new { message = "File not found" });
            }

            System.IO.File.Delete(filePath);
            _logger.LogInformation("Deleted boot file: {Path}", path);

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error deleting boot file");
            return StatusCode(500, new { message = "Error deleting boot file" });
        }
    }

    private static string GetArchitecture(string filePath) {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath)?.Split(Path.DirectorySeparatorChar).LastOrDefault()?.ToLowerInvariant();

        if (directory == "arm64" || fileName.Contains("arm64")) return "ARM64";
        if (directory == "uefi" || fileName.EndsWith(".efi")) return "UEFI";
        if (directory == "bios" || fileName.EndsWith(".kpxe") || fileName.EndsWith(".pxe")) return "BIOS";
        
        return "Unknown";
    }

    private static string GetArchitectureFromFileName(string fileName) {
        var lower = fileName.ToLowerInvariant();
        if (lower.Contains("arm64")) return "ARM64";
        if (lower.EndsWith(".efi")) return "UEFI";
        if (lower.EndsWith(".kpxe") || lower.EndsWith(".pxe")) return "BIOS";
        return "BIOS";
    }
}

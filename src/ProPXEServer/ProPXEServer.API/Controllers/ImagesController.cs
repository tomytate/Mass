using Microsoft.AspNetCore.Mvc;

namespace ProPXEServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ILogger<ImagesController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _imagesRoot;

    public ImagesController(ILogger<ImagesController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _imagesRoot = Path.Combine(_environment.WebRootPath, "images");
    }

    [HttpGet]
    public IActionResult ListImages()
    {
        try
        {
            var images = new List<object>();

            if (!Directory.Exists(_imagesRoot))
            {
                return Ok(images);
            }

            var categories = new[] { "usb", "iso", "disk" };

            foreach (var category in categories)
            {
                var categoryPath = Path.Combine(_imagesRoot, category);
                if (!Directory.Exists(categoryPath)) continue;

                var files = Directory.GetFiles(categoryPath);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    images.Add(new
                    {
                        id = images.Count + 1,
                        fileName = fileInfo.Name,
                        category = category.ToUpper(),
                        fileSizeBytes = fileInfo.Length,
                        fileSizeMB = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2),
                        downloadUrl = $"/api/images/{category}/{fileInfo.Name}",
                        createdAt = fileInfo.CreationTimeUtc
                    });
                }
            }

            return Ok(images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing distribution images");
            return StatusCode(500, "Error listing images");
        }
    }

    [HttpGet("usb/{filename}")]
    public IActionResult GetUsbImage(string filename)
    {
        return ServeImageFile("usb", filename);
    }

    [HttpGet("iso/{filename}")]
    public IActionResult GetIsoImage(string filename)
    {
        return ServeImageFile("iso", filename);
    }

    [HttpGet("disk/{filename}")]
    public IActionResult GetDiskImage(string filename)
    {
        return ServeImageFile("disk", filename);
    }

    private IActionResult ServeImageFile(string category, string filename)
    {
        try
        {
            var sanitizedFilename = Path.GetFileName(filename);
            var filePath = Path.Combine(_imagesRoot, category, sanitizedFilename);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Image file not found: {Category}/{Filename}", category, sanitizedFilename);
                return NotFound($"Image file '{sanitizedFilename}' not found in {category} category");
            }

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Serving {Category} image: {Filename} ({Size} MB)", 
                category, sanitizedFilename, Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2));

            var contentType = category switch
            {
                "iso" => "application/x-iso9660-image",
                "usb" or "disk" => "application/octet-stream",
                _ => "application/octet-stream"
            };

            return PhysicalFile(filePath, contentType, sanitizedFilename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving {Category} image: {Filename}", category, filename);
            return StatusCode(500, "Error serving image file");
        }
    }
}


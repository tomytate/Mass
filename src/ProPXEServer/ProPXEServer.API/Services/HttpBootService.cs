using Microsoft.AspNetCore.StaticFiles;
using ProPXEServer.API.Data;

namespace ProPXEServer.API.Services;

public class HttpBootService(
    ILogger<HttpBootService> logger,
    IConfiguration configuration,
    IServiceProvider serviceProvider) {
    
    private readonly string _bootFilesDirectory = configuration["ProPXEServer:BootFilesDirectory"] ?? "BootFiles";

    public async Task<IResult> ServeBootFile(string fileName, HttpContext context) {
        try {
            var filePath = Path.Combine(_bootFilesDirectory, fileName.TrimStart('/'));
            
            if (!File.Exists(filePath)) {
                logger.LogWarning("Boot file not found: {FilePath}", filePath);
                return Results.NotFound();
            }
            
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            logger.LogInformation("HTTP boot request for {FileName} from {IP}", fileName, ipAddress);
            
            await LogPxeEvent("HTTP_BOOT", ipAddress, fileName);
            
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType)) {
                contentType = "application/octet-stream";
            }
            
            return Results.File(filePath, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error serving boot file: {FileName}", fileName);
            return Results.Problem("Error serving file");
        }
    }

    private async Task LogPxeEvent(string eventType, string ipAddress, string fileName) {
        try {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            dbContext.PxeEvents.Add(new PxeEvent {
                EventType = eventType,
                MacAddress = "unknown",
                IpAddress = ipAddress,
                BootFileName = fileName
            });
            
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to log PXE event");
        }
    }
}



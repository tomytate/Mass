using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Threading.RateLimiting;

namespace ProPXEServer.API.Security;

public static class SecurityConfiguration {
    public static IServiceCollection AddPxeSecurityPolicies(this IServiceCollection services, IConfiguration configuration) {
        
        
        services.AddRateLimiter(options => {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                
                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions {
                    PermitLimit = 100,              
                    Window = TimeSpan.FromMinutes(1), 
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
            });

            options.OnRejected = async (context, token) => {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
            };
        });

        return services;
    }

    
    public static bool IsIpAllowed(string ipAddress, IConfiguration configuration) {
        var whitelist = configuration.GetSection("Security:IpWhitelist").Get<string[]>();
        var blacklist = configuration.GetSection("Security:IpBlacklist").Get<string[]>();

        if (blacklist != null && blacklist.Contains(ipAddress)) {
            return false;
        }

        if (whitelist != null && whitelist.Length > 0) {
            return whitelist.Contains(ipAddress);
        }

        return true;
    }

    
    public static bool IsValidMacAddress(string macAddress) {
        if (string.IsNullOrWhiteSpace(macAddress)) return false;
        
        var parts = macAddress.Split(':');
        if (parts.Length != 6) return false;

        foreach (var part in parts) {
            if (part.Length != 2) return false;
            if (!int.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out _)) {
                return false;
            }
        }

        return true;
    }

    
    public static void LogSecurityEvent(ILogger logger, string eventType, string? ipAddress, string? macAddress, string? details = null) {
        logger.LogWarning("SECURITY_EVENT: {EventType} | IP: {IP} | MAC: {MAC} | Details: {Details}", 
            eventType, ipAddress ?? "unknown", macAddress ?? "unknown", details ?? "none");
    }
}




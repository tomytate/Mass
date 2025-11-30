using Microsoft.AspNetCore.Identity;

namespace ProPXEServer.API.Data;

public class ApplicationUser : IdentityUser {
    public string? StripeCustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSubscribed { get; set; }
    public DateTime? SubscriptionExpiry { get; set; }
    public string? SubscriptionStatus { get; set; }
}



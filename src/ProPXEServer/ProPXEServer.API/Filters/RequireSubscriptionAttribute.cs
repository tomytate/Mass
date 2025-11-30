using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using ProPXEServer.API.Data;

namespace ProPXEServer.API.Filters;

public class RequireSubscriptionAttribute : TypeFilterAttribute {
    public RequireSubscriptionAttribute() : base(typeof(RequireSubscriptionFilter)) { }
}

public class RequireSubscriptionFilter(
    UserManager<ApplicationUser> userManager,
    ILogger<RequireSubscriptionFilter> logger) : IAsyncActionFilter {
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated ?? true) {
            context.Result = new UnauthorizedResult();
            return;
        }

        var applicationUser = await userManager.GetUserAsync(user);
        if (applicationUser == null) {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!applicationUser.IsSubscribed || 
            applicationUser.SubscriptionStatus != "active" ||
            (applicationUser.SubscriptionExpiry.HasValue && 
             applicationUser.SubscriptionExpiry.Value < DateTime.UtcNow)) {
            
            logger.LogWarning("User {UserId} attempted to access subscription-required endpoint without valid subscription", 
                applicationUser.Id);
            
            context.Result = new ObjectResult(new { 
                message = "Active subscription required",
                subscriptionRequired = true
            }) {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}



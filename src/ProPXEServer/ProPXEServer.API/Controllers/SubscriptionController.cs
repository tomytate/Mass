using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using ProPXEServer.API.Data;
using Asp.Versioning;

namespace ProPXEServer.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SubscriptionController(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<SubscriptionController> logger) : ControllerBase {

    private readonly string _stripeSecretKey = configuration["Stripe:SecretKey"] 
        ?? throw new InvalidOperationException("Stripe secret key not configured");
    private readonly string _monthlyPriceId = configuration["Stripe:MonthlyPriceId"] 
        ?? throw new InvalidOperationException("Stripe price ID not configured");

    [HttpGet("status")]
    public async Task<IActionResult> GetSubscriptionStatus() {
        try {
            var user = await userManager.GetUserAsync(User);
            if (user == null) {
                return Unauthorized();
            }

            return Ok(new {
                user.IsSubscribed,
                user.SubscriptionStatus,
                user.SubscriptionExpiry,
                user.StripeCustomerId
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting subscription status");
            return StatusCode(500, new { message = "Error fetching subscription status" });
        }
    }

    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckoutSession() {
        try {
            var user = await userManager.GetUserAsync(User);
            if (user == null) {
                return Unauthorized();
            }

            StripeConfiguration.ApiKey = _stripeSecretKey;

            string? customerId = user.StripeCustomerId;
            
            if (string.IsNullOrEmpty(customerId)) {
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(new CustomerCreateOptions {
                    Email = user.Email,
                    Metadata = new Dictionary<string, string> {
                        { "UserId", user.Id }
                    }
                });
                customerId = customer.Id;
                
                user.StripeCustomerId = customerId;
                await userManager.UpdateAsync(user);
            }

            var options = new SessionCreateOptions {
                Customer = customerId,
                PaymentMethodTypes = ["card"],
                LineItems = [
                    new SessionLineItemOptions {
                        Price = _monthlyPriceId,
                        Quantity = 1
                    }
                ],
                Mode = "subscription",
                SuccessUrl = configuration["Stripe:SuccessUrl"] ?? "http://localhost:7001/subscription/success",
                CancelUrl = configuration["Stripe:CancelUrl"] ?? "http://localhost:7001/subscription/cancel",
                Metadata = new Dictionary<string, string> {
                    { "UserId", user.Id }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            logger.LogInformation("Created checkout session for user {UserId}: {SessionId}", 
                user.Id, session.Id);

            return Ok(new { 
                sessionId = session.Id,
                url = session.Url
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error creating checkout session");
            return StatusCode(500, new { message = "Error creating checkout session" });
        }
    }

    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortalSession() {
        try {
            var user = await userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.StripeCustomerId)) {
                return BadRequest(new { message = "No active subscription" });
            }

            StripeConfiguration.ApiKey = _stripeSecretKey;

            var options = new Stripe.BillingPortal.SessionCreateOptions {
                Customer = user.StripeCustomerId,
                ReturnUrl = configuration["Stripe:SuccessUrl"] ?? "http://localhost:7001/subscription/success"
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new { url = session.Url });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error creating portal session");
            return StatusCode(500, new { message = "Error creating portal session" });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook() {
        try {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var webhookSecret = configuration["Stripe:WebhookSecret"];

            Event stripeEvent;
            try {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret,
                    throwOnApiVersionMismatch: true
                );
            }
            catch (StripeException ex) {
                logger.LogWarning(ex, "Invalid Stripe webhook signature from {IP}", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized("Invalid signature");
            }
            catch (Exception ex) {
                logger.LogError(ex, "Webhook signature verification failed");
                return BadRequest();
            }

            logger.LogInformation("Processing Stripe webhook: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type) {
                case "checkout.session.completed":
                    await HandleCheckoutCompleted(stripeEvent);
                    break;
                
                case "customer.subscription.updated":
                case "customer.subscription.created":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;
                
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
                
                case "invoice.payment_succeeded":
                    await HandlePaymentSucceeded(stripeEvent);
                    break;
                
                case "invoice.payment_failed":
                    await HandlePaymentFailed(stripeEvent);
                    break;
            }

            return Ok();
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error handling webhook");
            return StatusCode(500);
        }
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent) {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var userId = session.Metadata["UserId"];
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.IsSubscribed = true;
        user.SubscriptionStatus = "active";
        await userManager.UpdateAsync(user);

        logger.LogInformation("Checkout completed for user {UserId}", userId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent) {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        StripeConfiguration.ApiKey = _stripeSecretKey;
        var customerService = new CustomerService();
        var customer = await customerService.GetAsync(subscription.CustomerId);

        if (!customer.Metadata.TryGetValue("UserId", out var userId)) return;

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.IsSubscribed = subscription.Status == "active";
        user.SubscriptionStatus = subscription.Status;
        user.SubscriptionExpiry = subscription.CurrentPeriodEnd;
        await userManager.UpdateAsync(user);

        logger.LogInformation("Subscription updated for user {UserId}: {Status}", 
            userId, subscription.Status);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent) {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        StripeConfiguration.ApiKey = _stripeSecretKey;
        var customerService = new CustomerService();
        var customer = await customerService.GetAsync(subscription.CustomerId);

        if (!customer.Metadata.TryGetValue("UserId", out var userId)) return;

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.IsSubscribed = false;
        user.SubscriptionStatus = "canceled";
        await userManager.UpdateAsync(user);

        logger.LogInformation("Subscription deleted for user {UserId}", userId);
    }

    private async Task HandlePaymentSucceeded(Event stripeEvent) {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        logger.LogInformation("Payment succeeded for customer {CustomerId}", 
            invoice.CustomerId);
    }

    private async Task HandlePaymentFailed(Event stripeEvent) {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        logger.LogWarning("Payment failed for customer {CustomerId}", 
            invoice.CustomerId);
    }
}




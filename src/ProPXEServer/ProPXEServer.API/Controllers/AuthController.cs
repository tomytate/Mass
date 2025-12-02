using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProPXEServer.API.Data;

namespace ProPXEServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase {

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request) {
        try {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null) {
                return BadRequest(new { message = "Email already registered" });
            }

            var user = new ApplicationUser {
                UserName = request.Email,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                IsSubscribed = false
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded) {
                return BadRequest(new { 
                    message = "Registration failed", 
                    errors = result.Errors.Select(e => e.Description) 
                });
            }

            logger.LogInformation("New user registered: {Email}", request.Email);

            var token = GenerateJwtToken(user);
            return Ok(new {
                token,
                user = new {
                    user.Id,
                    user.Email,
                    user.IsSubscribed
                }
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Registration error for {Email}", request.Email);
            return StatusCode(500, new { message = "Registration failed" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) {
        try {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null) {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded) {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            logger.LogInformation("User logged in: {Email}", request.Email);

            var token = GenerateJwtToken(user);
            return Ok(new {
                token,
                user = new {
                    user.Id,
                    user.Email,
                    user.IsSubscribed,
                    user.SubscriptionStatus,
                    user.SubscriptionExpiry
                }
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Login error for {Email}", request.Email);
            return StatusCode(500, new { message = "Login failed" });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser() {
        try {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) {
                return Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null) {
                return NotFound();
            }

            return Ok(new {
                user.Id,
                user.Email,
                user.IsSubscribed,
                user.SubscriptionStatus,
                user.SubscriptionExpiry,
                user.CreatedAt
            });
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Error fetching user data" });
        }
    }

    private string GenerateJwtToken(ApplicationUser user) {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        
        if (string.IsNullOrEmpty(secretKey)) {
            logger.LogCritical("SECURITY: JWT secret key not configured");
            throw new InvalidOperationException("Authentication disabled - JWT secret missing");
        }
        
        if (secretKey.Length < 32) {
            throw new InvalidOperationException("JWT secret must be at least 32 characters");
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("IsSubscribed", user.IsSubscribed.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);



using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Threading.RateLimiting;
using ProPXEServer.API.Data;
using ProPXEServer.API.Services;
using ProPXEServer.API.Security;
using Mass.Core.Configuration;

using Asp.Versioning;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});

if (builder.Environment.IsProduction())
{
    SecureConfiguration.ValidateProductionSecrets(
        "JwtSettings:SecretKey",
        "Stripe:SecretKey",
        "Stripe:WebhookSecret"
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=ProPXEServer.db"));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var jwtSecret = SecureConfiguration.GetSecretOrDefault("JwtSettings:SecretKey") 
    ?? builder.Configuration["JwtSettings:SecretKey"];

if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            "JWT secret must be at least 32 characters. Set MASS_JWTSETTINGS__SECRETKEY environment variable.");
    }
    
    jwtSecret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + 
                Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    Console.WriteLine("WARNING: Using auto-generated JWT secret. Set MASS_JWTSETTINGS__SECRETKEY for production.");
}

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddOutputCache(options => 
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddOpenApi();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? (builder.Environment.IsDevelopment() 
        ? ["http://localhost:5173", "http://localhost:7001", "https://localhost:7001"]
        : []);

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options => {
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions {
                AutoReplenishment = true,
                PermitLimit = builder.Configuration.GetValue("Security:MaxRequestsPerMinute", 100),
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddSingleton<HttpBootService>();
builder.Services.AddHostedService<DhcpService>();
builder.Services.AddHostedService<TftpServerService>();
builder.Services.AddPxeSecurityPolicies(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseCors();
app.UseRateLimiter();

if (!app.Environment.IsDevelopment()) {
    app.UseHttpsRedirection();
}

app.UseResponseCompression();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseOutputCache();

app.MapGet("/boot/{**fileName}", async (string fileName, HttpBootService bootService, HttpContext context) => {
    return await bootService.ServeBootFile(fileName, context);
});

app.MapGet("/api/pxe/events", async (ApplicationDbContext db) => {
    return await db.PxeEvents
        .OrderByDescending(e => e.Timestamp)
        .Take(100)
        .ToListAsync();
}).RequireAuthorization();

app.Run();

using Mass.Dashboard.Components;
using Mass.Core.Interfaces;
using Mass.Core.Logging;
using Mass.Core.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Add Mass.Core services
builder.Services.AddSingleton<ILogService, FileLogService>();
builder.Services.AddSingleton<WorkflowParser>();
builder.Services.AddSingleton<WorkflowValidator>();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

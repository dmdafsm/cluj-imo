using ClujImo.Infrastructure;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Web.Workers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IngestionTrigger>();
builder.Services.AddHostedService<IngestionWorker>();
builder.Services.AddHostedService<GeocodingWorker>();
builder.Services.AddRazorPages(options => options.Conventions.AddPageRoute("/Dashboard", ""));
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// Keys survive container restarts, so antiforgery cookies issued before a restart stay valid.
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.MapHealthChecks("/health");
app.MapRazorPages();

await app.RunAsync();

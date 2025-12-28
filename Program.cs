using FootballPointsApp.Data;
using FootballPointsApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

// Configure EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<PointsCalculatorService>();
builder.Services.AddScoped<MonthlyBonusService>();
builder.Services.AddSingleton<TimeService>();

var app = builder.Build();

// Log startup information
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogWarning("=== APPLICATION STARTING ===");
startupLogger.LogWarning($"Environment: {app.Environment.EnvironmentName}");
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connString))
{
    try
    {
        var csBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connString);
        startupLogger.LogWarning($"DB Host: {csBuilder.Host}");
        startupLogger.LogWarning($"DB Port: {csBuilder.Port}");
        startupLogger.LogWarning($"DB Name: {csBuilder.Database}");
        startupLogger.LogWarning($"DB User: {csBuilder.Username}");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to parse connection string");
    }
}
else
{
    startupLogger.LogError("Connection string is NULL or EMPTY!");
}

// Configure forwarded headers (important for reverse proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

// Configure path base for subdirectory deployment
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

// App-side PathBase support when reverse proxy strips the prefix
// - If nginx does not send X-Forwarded-Prefix and rewrites /futpoints -> /
//   we still want generated URLs to include /futpoints on specific hosts.
var pathBaseHosts = builder.Configuration.GetSection("PathBaseHosts").Get<string[]>() ?? Array.Empty<string>();
if (!string.IsNullOrEmpty(pathBase))
{
    app.Use((context, next) =>
    {
        // Prefer X-Forwarded-Prefix when present
        if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var forwardedPrefix) && !string.IsNullOrEmpty(forwardedPrefix))
        {
            if (!context.Request.PathBase.HasValue)
            {
                context.Request.PathBase = new PathString(forwardedPrefix.ToString());
            }
            return next();
        }

        // Otherwise apply PathBase for configured hosts
        var host = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        if (string.IsNullOrEmpty(host)) host = context.Request.Host.Value;

        if (pathBaseHosts.Length > 0)
        {
            foreach (var h in pathBaseHosts)
            {
                if (!string.IsNullOrWhiteSpace(h) && host.EndsWith(h, StringComparison.OrdinalIgnoreCase))
                {
                    if (!context.Request.PathBase.HasValue)
                    {
                        context.Request.PathBase = new PathString(pathBase);
                    }
                    break;
                }
            }
        }

        return next();
    });
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var finalLogger = app.Services.GetRequiredService<ILogger<Program>>();
finalLogger.LogWarning("=== ROUTING CONFIGURED, STARTING APPLICATION ===");

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    logger.LogWarning("=== DATABASE MIGRATION STARTING ===");
    
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        logger.LogWarning("DbContext created successfully");
        
        var connString = builder.Configuration.GetConnectionString("DefaultConnection");
        logger.LogWarning($"Using connection string: {connString?.Substring(0, Math.Min(50, connString?.Length ?? 0))}...");
        
        // Wait for DB to be ready
        var canConnect = false;
        for (int i = 0; i < 10; i++)
        {
            logger.LogWarning($"Connection attempt {i + 1}/10...");
            try
            {
                if (context.Database.CanConnect())
                {
                    canConnect = true;
                    logger.LogWarning("✓ Database connection SUCCESSFUL!");
                    break;
                }
                else
                {
                    logger.LogWarning($"CanConnect returned false on attempt {i + 1}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"✗ Connection attempt {i + 1}/10 FAILED: {ex.Message}");
                if (ex.InnerException != null)
                {
                    logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
            }
            System.Threading.Thread.Sleep(2000);
        }
        
        if (canConnect)
        {
            logger.LogWarning("Applying migrations...");
            context.Database.Migrate();
            logger.LogWarning("✓ Migrations applied successfully!");
        }
        else
        {
            logger.LogError("✗✗✗ Could not connect to database after 10 attempts ✗✗✗");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "✗✗✗ FATAL ERROR during database migration ✗✗✗");
        logger.LogError($"Exception type: {ex.GetType().Name}");
        logger.LogError($"Message: {ex.Message}");
        if (ex.InnerException != null)
        {
            logger.LogError($"Inner exception: {ex.InnerException.Message}");
        }
        logger.LogError($"Stack trace: {ex.StackTrace}");
    }
    
    logger.LogWarning("=== DATABASE MIGRATION COMPLETED ===");
}

logger.LogWarning("=== CALLING app.Run() - HTTP SERVER STARTING ===");
app.Run();
logger.LogWarning("=== app.Run() RETURNED - APPLICATION STOPPED ===");

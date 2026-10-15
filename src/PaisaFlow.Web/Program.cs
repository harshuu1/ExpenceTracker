using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;
using PaisaFlow.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Authentication via Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

// Connection string prioritization (Matches jobApply architecture)
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? builder.Configuration.GetConnectionString("SqliteFallback")
           ?? "Data Source=paisaflow.db";

var isPostgres = rawConn.Contains("Host=", StringComparison.OrdinalIgnoreCase) 
              || rawConn.Contains("postgres", StringComparison.OrdinalIgnoreCase) 
              || rawConn.Contains("neon.tech", StringComparison.OrdinalIgnoreCase);

var isSqlite = rawConn.Contains(".db", StringComparison.OrdinalIgnoreCase);

if (!isPostgres && !isSqlite && !rawConn.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
{
    rawConn += ";TrustServerCertificate=True";
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isPostgres)
    {
        options.UseNpgsql(rawConn, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(3), errorCodesToAdd: null);
        });
    }
    else if (isSqlite)
    {
        options.UseSqlite(rawConn);
    }
    else
    {
        options.UseSqlServer(rawConn, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(3), errorNumbersToAdd: null);
        });
    }
});

// Register Core Domain Services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IQuickAddParser, QuickAddParserService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<ISplitExpenseService, SplitExpenseService>();
builder.Services.AddScoped<IExportService, ExportService>();

// Configure Forwarded Headers for Cloud Hosting (Render, Docker, Linux, Nginx)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Auto-migrate & seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    try
    {
        logger.LogInformation("Connecting to database: {Conn}", rawConn);
        await DbInitializer.InitializeAsync(context);
        logger.LogInformation("PaisaFlow Database initialized and verified successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database: {Message}", ex.Message);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllers();

app.Run();

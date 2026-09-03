using System;
using ITRepairService.Data;
using ITRepairService.Models;
using ITRepairService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SQLite";
var sqliteConnection = builder.Configuration.GetConnectionString("SQLite")
    ?? builder.Configuration.GetConnectionString("SqliteConnection")
    ?? throw new InvalidOperationException("Connection string 'SQLite' is missing.");
var sqlServerConnection = builder.Configuration.GetConnectionString("SQLServer")
    ?? builder.Configuration.GetConnectionString("SqlServerConnection")
    ?? throw new InvalidOperationException("Connection string 'SQLServer' is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(sqlServerConnection);
        // The model snapshot in the migrations assembly is maintained manually and can
        // lag behind the model; ignore the pending-model-changes check on startup.
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
    else
    {
        options.UseSqlite(sqliteConnection);
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Register LDAP authentication service
builder.Services.Configure<LdapSettings>(builder.Configuration.GetSection("LDAP"));
builder.Services.AddSingleton<ILdapAuthenticationService, LdapAuthenticationService>();

// Register Department Initializer service
builder.Services.AddScoped<IDepartmentInitializerService, DepartmentInitializerService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

// Apply pending migrations and create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

await IdentitySeeder.SeedRolesAndAdminAsync(app.Services, app.Configuration);

// Seed test users for testing role assignment logic
await IdentitySeeder.SeedTestUsersAsync(app.Services);

app.Run();


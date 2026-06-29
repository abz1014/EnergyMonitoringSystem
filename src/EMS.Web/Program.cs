using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;
using EMS.Web.Services;
using EMS.Web.Validators;
using EMS.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("ScadaDb")
    ?? throw new InvalidOperationException("Connection string 'ScadaDb' not found in configuration.");

builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity configuration
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ScadaDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Repository dependency injection
builder.Services.AddScoped<IEnergyMeterRepository, EnergyMeterRepository>();
builder.Services.AddScoped<IEnergyMeterLiveRepository, EnergyMeterLiveRepository>();
builder.Services.AddScoped<IMonitoringDeviceRepository, MonitoringDeviceRepository>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<IFlowmeterRepository, FlowmeterRepository>();

// Service dependency injection
builder.Services.AddScoped<IDashboardService, WebDashboardService>();
builder.Services.AddScoped<ILiveMonitoringService, LiveMonitoringService>();

// Role and user seeding
builder.Services.AddScoped<RoleSeederService>();

// Validation
builder.Services.AddScoped<IValidator<DashboardFilterDto>, DashboardFilterValidator>();
builder.Services.AddScoped<IValidator<LiveMonitoringFilterDto>, LiveMonitoringFilterValidator>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Security headers
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(); // HTTP Strict-Transport-Security
}

app.UseHttpsRedirection(); // Enforce HTTPS
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles and default users
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<RoleSeederService>();
    await seeder.InitializeRolesAsync();
}

app.Run();

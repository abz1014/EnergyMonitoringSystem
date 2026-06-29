using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("ScadaDb")
    ?? "Server=(local)\\SQLEXPRESS;Database=db_SCADA;Integrated Security=true;TrustServerCertificate=true;";

builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repository dependency injection
builder.Services.AddScoped<IEnergyMeterRepository, EnergyMeterRepository>();
builder.Services.AddScoped<IEnergyMeterLiveRepository, EnergyMeterLiveRepository>();
builder.Services.AddScoped<IMonitoringDeviceRepository, MonitoringDeviceRepository>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<IFlowmeterRepository, FlowmeterRepository>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

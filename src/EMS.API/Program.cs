using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;
using EMS.API.Services;

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

// Service dependency injection
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILiveMonitoringService, LiveMonitoringService>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

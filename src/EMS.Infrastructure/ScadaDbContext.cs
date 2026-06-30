namespace EMS.Infrastructure.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EMS.Core.Models;

public class ScadaDbContext : IdentityDbContext<AppUser>
{
    public ScadaDbContext(DbContextOptions<ScadaDbContext> options) : base(options)
    {
    }

    public DbSet<EnergyMeterData> EnergyMetersData { get; set; }
    public DbSet<MonitoringDevice> MonitoringDevices { get; set; }
    public DbSet<Alarm> Alarms { get; set; }
    public DbSet<FlowmeterData> FlowmetersData { get; set; }
    public DbSet<DeviceTag> DeviceTags { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<DailyTemperature> DailyTemperatures { get; set; }
    public DbSet<UserDashboardWidget> UserDashboardWidgets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Table mappings
        modelBuilder.Entity<EnergyMeterData>().ToTable("tblEnergyMetersData");
        modelBuilder.Entity<MonitoringDevice>().ToTable("tblMonitoringDevices");
        modelBuilder.Entity<Alarm>().ToTable("Alarms");
        modelBuilder.Entity<FlowmeterData>().ToTable("tbFlowmetersData");
        modelBuilder.Entity<DeviceTag>().ToTable("tblDevicesTags");
        modelBuilder.Entity<AppSetting>().ToTable("tblAppSettings");
        modelBuilder.Entity<DailyTemperature>().ToTable("tblDailyTemperature");
        modelBuilder.Entity<UserDashboardWidget>().ToTable("tblUserDashboardWidgets");

        // EnergyMeterData configurations
        modelBuilder.Entity<EnergyMeterData>()
            .HasKey(e => e.SrNo);
        modelBuilder.Entity<EnergyMeterData>()
            .HasIndex(e => new { e.MeterNo, e.DateTime })
            .HasDatabaseName("IX_EnergyMetersData_MeterNo_DateTime");
        modelBuilder.Entity<EnergyMeterData>()
            .HasIndex(e => e.DateTime)
            .HasDatabaseName("IX_EnergyMetersData_DateTime");

        // MonitoringDevice configurations
        modelBuilder.Entity<MonitoringDevice>()
            .HasKey(e => e.SrNo);
        modelBuilder.Entity<MonitoringDevice>()
            .HasIndex(e => e.DeviceID)
            .HasDatabaseName("IX_MonitoringDevices_DeviceID");
        modelBuilder.Entity<MonitoringDevice>()
            .HasIndex(e => e.GroupName)
            .HasDatabaseName("IX_MonitoringDevices_GroupName");

        // Alarm configurations
        modelBuilder.Entity<Alarm>()
            .HasKey(e => e.AlarmID);
        modelBuilder.Entity<Alarm>()
            .HasIndex(e => new { e.IsActive, e.CreatedAt })
            .HasDatabaseName("IX_Alarms_IsActive_CreatedAt");

        // FlowmeterData configurations
        modelBuilder.Entity<FlowmeterData>()
            .HasKey(e => e.SrNo);
        modelBuilder.Entity<FlowmeterData>()
            .HasIndex(e => new { e.MeterNo, e.DateTime })
            .HasDatabaseName("IX_FlowmetersData_MeterNo_DateTime");

        // AppSetting configurations
        modelBuilder.Entity<AppSetting>()
            .HasKey(e => e.SettingKey);

        modelBuilder.Entity<DailyTemperature>()
            .HasKey(e => e.TempDate);

        modelBuilder.Entity<UserDashboardWidget>()
            .HasKey(e => e.SrNo);
        modelBuilder.Entity<UserDashboardWidget>()
            .HasIndex(e => new { e.UserId, e.WidgetKey })
            .IsUnique();

        // DeviceTag configurations
        modelBuilder.Entity<DeviceTag>()
            .HasKey(e => e.SrNo);
        modelBuilder.Entity<DeviceTag>()
            .HasIndex(e => e.TagName)
            .HasDatabaseName("IX_DevicesTags_TagName");
    }
}

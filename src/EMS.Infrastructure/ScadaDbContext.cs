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
    public DbSet<EnergyMeterLive> EnergyMeterLive { get; set; }
    public DbSet<MonitoringDevice> MonitoringDevices { get; set; }
    public DbSet<Alarm> Alarms { get; set; }
    public DbSet<FlowmeterData> FlowmetersData { get; set; }
    public DbSet<DeviceTag> DeviceTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Table mappings
        modelBuilder.Entity<EnergyMeterData>().ToTable("tblEnergyMetersData");
        modelBuilder.Entity<EnergyMeterLive>().ToTable("tblEnergyMeterLive");
        modelBuilder.Entity<MonitoringDevice>().ToTable("tblMonitoringDevices");
        modelBuilder.Entity<Alarm>().ToTable("Alarms");
        modelBuilder.Entity<FlowmeterData>().ToTable("tbFlowmetersData");
        modelBuilder.Entity<DeviceTag>().ToTable("tblDevicesTags");

        // EnergyMeterData configurations
        modelBuilder.Entity<EnergyMeterData>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<EnergyMeterData>()
            .HasIndex(e => new { e.MeterNo, e.DateTime })
            .HasDatabaseName("IX_EnergyMetersData_MeterNo_DateTime");
        modelBuilder.Entity<EnergyMeterData>()
            .HasIndex(e => e.DateTime)
            .HasDatabaseName("IX_EnergyMetersData_DateTime");

        // EnergyMeterLive configurations
        modelBuilder.Entity<EnergyMeterLive>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<EnergyMeterLive>()
            .HasIndex(e => e.MeterNo)
            .IsUnique()
            .HasDatabaseName("IX_EnergyMeterLive_MeterNo");

        // MonitoringDevice configurations
        modelBuilder.Entity<MonitoringDevice>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<MonitoringDevice>()
            .HasIndex(e => e.DeviceID)
            .HasDatabaseName("IX_MonitoringDevices_DeviceID");
        modelBuilder.Entity<MonitoringDevice>()
            .HasIndex(e => new { e.Plant, e.Building })
            .HasDatabaseName("IX_MonitoringDevices_Plant_Building");

        // Alarm configurations
        modelBuilder.Entity<Alarm>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<Alarm>()
            .HasIndex(e => new { e.IsActive, e.CreatedAt })
            .HasDatabaseName("IX_Alarms_IsActive_CreatedAt");

        // FlowmeterData configurations
        modelBuilder.Entity<FlowmeterData>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<FlowmeterData>()
            .HasIndex(e => new { e.DeviceID, e.DateTime })
            .HasDatabaseName("IX_FlowmetersData_DeviceID_DateTime");

        // DeviceTag configurations
        modelBuilder.Entity<DeviceTag>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<DeviceTag>()
            .HasIndex(e => e.DeviceID)
            .HasDatabaseName("IX_DevicesTags_DeviceID");
    }
}

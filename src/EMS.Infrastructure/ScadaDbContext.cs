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
            .HasIndex(e => e.DateTime);
        modelBuilder.Entity<EnergyMeterData>()
            .HasIndex(e => e.MeterNo);

        // EnergyMeterLive configurations
        modelBuilder.Entity<EnergyMeterLive>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<EnergyMeterLive>()
            .HasIndex(e => e.MeterNo);

        // MonitoringDevice configurations
        modelBuilder.Entity<MonitoringDevice>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<MonitoringDevice>()
            .HasIndex(e => e.DeviceID);

        // Alarm configurations
        modelBuilder.Entity<Alarm>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<Alarm>()
            .HasIndex(e => e.IsActive);

        // FlowmeterData configurations
        modelBuilder.Entity<FlowmeterData>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<FlowmeterData>()
            .HasIndex(e => e.DeviceID);

        // DeviceTag configurations
        modelBuilder.Entity<DeviceTag>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<DeviceTag>()
            .HasIndex(e => e.DeviceID);
    }
}

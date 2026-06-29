using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tblEnergyMetersData_MeterNo",
                table: "tblEnergyMetersData");

            migrationBuilder.DropIndex(
                name: "IX_tblEnergyMeterLive_MeterNo",
                table: "tblEnergyMeterLive");

            migrationBuilder.DropIndex(
                name: "IX_tbFlowmetersData_DeviceID",
                table: "tbFlowmetersData");

            migrationBuilder.DropIndex(
                name: "IX_Alarms_IsActive",
                table: "Alarms");

            migrationBuilder.RenameIndex(
                name: "IX_tblMonitoringDevices_DeviceID",
                table: "tblMonitoringDevices",
                newName: "IX_MonitoringDevices_DeviceID");

            migrationBuilder.RenameIndex(
                name: "IX_tblEnergyMetersData_DateTime",
                table: "tblEnergyMetersData",
                newName: "IX_EnergyMetersData_DateTime");

            migrationBuilder.RenameIndex(
                name: "IX_tblDevicesTags_DeviceID",
                table: "tblDevicesTags",
                newName: "IX_DevicesTags_DeviceID");

            migrationBuilder.AlterColumn<string>(
                name: "Plant",
                table: "tblMonitoringDevices",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Building",
                table: "tblMonitoringDevices",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringDevices_Plant_Building",
                table: "tblMonitoringDevices",
                columns: new[] { "Plant", "Building" });

            migrationBuilder.CreateIndex(
                name: "IX_EnergyMetersData_MeterNo_DateTime",
                table: "tblEnergyMetersData",
                columns: new[] { "MeterNo", "DateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EnergyMeterLive_MeterNo",
                table: "tblEnergyMeterLive",
                column: "MeterNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowmetersData_DeviceID_DateTime",
                table: "tbFlowmetersData",
                columns: new[] { "DeviceID", "DateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_IsActive_CreatedAt",
                table: "Alarms",
                columns: new[] { "IsActive", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MonitoringDevices_Plant_Building",
                table: "tblMonitoringDevices");

            migrationBuilder.DropIndex(
                name: "IX_EnergyMetersData_MeterNo_DateTime",
                table: "tblEnergyMetersData");

            migrationBuilder.DropIndex(
                name: "IX_EnergyMeterLive_MeterNo",
                table: "tblEnergyMeterLive");

            migrationBuilder.DropIndex(
                name: "IX_FlowmetersData_DeviceID_DateTime",
                table: "tbFlowmetersData");

            migrationBuilder.DropIndex(
                name: "IX_Alarms_IsActive_CreatedAt",
                table: "Alarms");

            migrationBuilder.RenameIndex(
                name: "IX_MonitoringDevices_DeviceID",
                table: "tblMonitoringDevices",
                newName: "IX_tblMonitoringDevices_DeviceID");

            migrationBuilder.RenameIndex(
                name: "IX_EnergyMetersData_DateTime",
                table: "tblEnergyMetersData",
                newName: "IX_tblEnergyMetersData_DateTime");

            migrationBuilder.RenameIndex(
                name: "IX_DevicesTags_DeviceID",
                table: "tblDevicesTags",
                newName: "IX_tblDevicesTags_DeviceID");

            migrationBuilder.AlterColumn<string>(
                name: "Plant",
                table: "tblMonitoringDevices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Building",
                table: "tblMonitoringDevices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_tblEnergyMetersData_MeterNo",
                table: "tblEnergyMetersData",
                column: "MeterNo");

            migrationBuilder.CreateIndex(
                name: "IX_tblEnergyMeterLive_MeterNo",
                table: "tblEnergyMeterLive",
                column: "MeterNo");

            migrationBuilder.CreateIndex(
                name: "IX_tbFlowmetersData_DeviceID",
                table: "tbFlowmetersData",
                column: "DeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_IsActive",
                table: "Alarms",
                column: "IsActive");
        }
    }
}

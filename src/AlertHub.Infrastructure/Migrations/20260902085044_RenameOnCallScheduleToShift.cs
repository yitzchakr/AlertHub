using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameOnCallScheduleToShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF scaffolds an entity rename as DropTable + CreateTable, which would discard
            // every existing shift. Renaming in place preserves the rows.
            migrationBuilder.RenameTable(
                name: "OnCallSchedules",
                newName: "OnCallShifts");

            // The table already carries its new name, so these reference "OnCallShifts".
            migrationBuilder.RenameIndex(
                name: "IX_OnCallSchedules_ServiceId",
                table: "OnCallShifts",
                newName: "IX_OnCallShifts_ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_OnCallSchedules_TenantId",
                table: "OnCallShifts",
                newName: "IX_OnCallShifts_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_OnCallSchedules_UserId",
                table: "OnCallShifts",
                newName: "IX_OnCallShifts_UserId");

            // sp_rename leaves the primary key and foreign keys under their old names, while
            // the model snapshot now expects the new ones. Without this, a later migration
            // that drops or recreates them would target constraints that do not exist.
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_OnCallSchedules]', N'PK_OnCallShifts', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_OnCallSchedules_Services_ServiceId]', N'FK_OnCallShifts_Services_ServiceId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_OnCallSchedules_Tenants_TenantId]', N'FK_OnCallShifts_Tenants_TenantId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_OnCallSchedules_Users_UserId]', N'FK_OnCallShifts_Users_UserId', N'OBJECT';");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "OnCallShifts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // Added nullable, backfilled, then tightened. Using a column default instead would
            // leave a stray DEFAULT constraint the model does not declare, and would silently
            // supply a timestamp for inserts that forgot one.
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "OnCallShifts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [OnCallShifts] SET [CreatedAt] = SYSUTCDATETIME() WHERE [CreatedAt] IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OnCallShifts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OnCallShifts");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "OnCallShifts");

            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_OnCallShifts_Users_UserId]', N'FK_OnCallSchedules_Users_UserId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_OnCallShifts_Tenants_TenantId]', N'FK_OnCallSchedules_Tenants_TenantId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_OnCallShifts_Services_ServiceId]', N'FK_OnCallSchedules_Services_ServiceId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_OnCallShifts]', N'PK_OnCallSchedules', N'OBJECT';");

            // Indexes are renamed while the table still carries its new name.
            migrationBuilder.RenameIndex(
                name: "IX_OnCallShifts_UserId",
                table: "OnCallShifts",
                newName: "IX_OnCallSchedules_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_OnCallShifts_TenantId",
                table: "OnCallShifts",
                newName: "IX_OnCallSchedules_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_OnCallShifts_ServiceId",
                table: "OnCallShifts",
                newName: "IX_OnCallSchedules_ServiceId");

            migrationBuilder.RenameTable(
                name: "OnCallShifts",
                newName: "OnCallSchedules");
        }
    }
}

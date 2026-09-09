using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitPermissionAndEscalationRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "PermissionRole");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "EscalationTierAssignments",
                newName: "EscalationRole");

            migrationBuilder.AddColumn<string>(
                name: "EscalationRole",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Every existing row carried one role doing both jobs, so the two start out
            // equal and nobody's paging changes the moment this runs. Without the backfill
            // each user would hold the "" default, which matches no EscalationRole value —
            // every role sweep would quietly resolve to nobody, and a tier that reaches
            // nobody is exactly the failure this system exists to prevent.
            migrationBuilder.Sql("UPDATE Users SET EscalationRole = PermissionRole;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalationRole",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "PermissionRole",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "EscalationRole",
                table: "EscalationTierAssignments",
                newName: "Role");
        }
    }
}

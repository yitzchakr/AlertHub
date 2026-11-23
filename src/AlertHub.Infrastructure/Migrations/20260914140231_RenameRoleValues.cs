using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertHub.Infrastructure.Migrations
{
    /// <summary>
    /// Data only — both columns are already nvarchar(50) holding the enum name, so the schema
    /// is unchanged and EF generated nothing. What changes is the set of legal values:
    /// PermissionRole loses Responder and Viewer, EscalationRole trades Admin for Manager and
    /// loses Viewer. Rows left holding a retired name would deserialize into nothing.
    /// </summary>
    public partial class RenameRoleValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Responder and Viewer were indistinguishable to every authorization check in the
            // codebase — only Admin was ever tested — so collapsing them changes no access.
            migrationBuilder.Sql(
                "UPDATE Users SET PermissionRole = 'Member' " +
                "WHERE PermissionRole IN ('Responder', 'Viewer');");

            // The source sets are disjoint, so these two run in either order.
            migrationBuilder.Sql(
                "UPDATE Users SET EscalationRole = 'Manager' WHERE EscalationRole = 'Admin';");

            // Nobody should have been paged as a Viewer, but the previous migration seeded
            // EscalationRole from PermissionRole, so anyone who was a Viewer became one here.
            // Responder is the only landing place that still answers a page.
            migrationBuilder.Sql(
                "UPDATE Users SET EscalationRole = 'Responder' WHERE EscalationRole = 'Viewer';");

            // Tier assignments target the same vocabulary and must move with it, or a tier
            // that pages 'Admin' silently matches nobody.
            migrationBuilder.Sql(
                "UPDATE EscalationTierAssignments SET EscalationRole = 'Manager' " +
                "WHERE EscalationRole = 'Admin';");

            migrationBuilder.Sql(
                "UPDATE EscalationTierAssignments SET EscalationRole = 'Responder' " +
                "WHERE EscalationRole = 'Viewer';");
        }

        /// <summary>
        /// Lossy, and deliberately so. Member was either a Responder or a Viewer and the
        /// distinction is gone, so everyone comes back as Responder — which is the safe
        /// direction, since Viewer never restricted anything the API checked.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Users SET PermissionRole = 'Responder' WHERE PermissionRole = 'Member';");

            migrationBuilder.Sql(
                "UPDATE Users SET EscalationRole = 'Admin' WHERE EscalationRole = 'Manager';");

            migrationBuilder.Sql(
                "UPDATE EscalationTierAssignments SET EscalationRole = 'Admin' " +
                "WHERE EscalationRole = 'Manager';");
        }
    }
}

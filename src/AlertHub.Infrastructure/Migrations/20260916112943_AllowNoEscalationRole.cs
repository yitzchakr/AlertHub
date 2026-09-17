using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowNoEscalationRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EscalationRole",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }

        /// <summary>
        /// Lossy: "no role sweep pages this person" cannot be said once the column is
        /// mandatory again, so those rows land on Responder. Paging somebody who asked not
        /// to be is the recoverable direction; the alternative is the "" default EF writes
        /// on its own, which matches no enum value and would drop them out of every sweep
        /// while looking like a real setting.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Users SET EscalationRole = 'Responder' WHERE EscalationRole IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "EscalationRole",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}

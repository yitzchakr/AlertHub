using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationExhaustionAndAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EscalationExhaustedAt",
                table: "Incidents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "IncidentEscalations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalationExhaustedAt",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "IncidentEscalations");
        }
    }
}

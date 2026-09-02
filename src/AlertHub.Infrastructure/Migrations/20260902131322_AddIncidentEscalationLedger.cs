using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentEscalationLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IncidentEscalations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentEscalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentEscalations_EscalationTiers_EscalationTierId",
                        column: x => x.EscalationTierId,
                        principalTable: "EscalationTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentEscalations_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentEscalationTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentEscalationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentEscalationTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentEscalationTargets_IncidentEscalations_IncidentEscalationId",
                        column: x => x.IncidentEscalationId,
                        principalTable: "IncidentEscalations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentEscalationTargets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEscalations_EscalationTierId",
                table: "IncidentEscalations",
                column: "EscalationTierId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEscalations_IncidentId_Level",
                table: "IncidentEscalations",
                columns: new[] { "IncidentId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEscalations_NotifiedAt",
                table: "IncidentEscalations",
                column: "NotifiedAt",
                filter: "[NotifiedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEscalationTargets_IncidentEscalationId_UserId",
                table: "IncidentEscalationTargets",
                columns: new[] { "IncidentEscalationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEscalationTargets_UserId",
                table: "IncidentEscalationTargets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentEscalationTargets");

            migrationBuilder.DropTable(
                name: "IncidentEscalations");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationShemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Services_ServiceId",
                table: "Incidents");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Services",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Services",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscalationPolicyId",
                table: "Services",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EscalationPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationPolicies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscalationTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    EscalateAfterMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationTiers_EscalationPolicies_EscalationPolicyId",
                        column: x => x.EscalationPolicyId,
                        principalTable: "EscalationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalationTiers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscalationTierAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationTierAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationTierAssignments_EscalationTiers_EscalationTierId",
                        column: x => x.EscalationTierId,
                        principalTable: "EscalationTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalationTierAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalationTierAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_EscalationPolicyId",
                table: "Services",
                column: "EscalationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationPolicies_TenantId",
                table: "EscalationPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationTierAssignments_EscalationTierId",
                table: "EscalationTierAssignments",
                column: "EscalationTierId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationTierAssignments_TenantId",
                table: "EscalationTierAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationTierAssignments_UserId",
                table: "EscalationTierAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationTiers_EscalationPolicyId",
                table: "EscalationTiers",
                column: "EscalationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationTiers_TenantId",
                table: "EscalationTiers",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Services_ServiceId",
                table: "Incidents",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_EscalationPolicies_EscalationPolicyId",
                table: "Services",
                column: "EscalationPolicyId",
                principalTable: "EscalationPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Services_ServiceId",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_EscalationPolicies_EscalationPolicyId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "EscalationTierAssignments");

            migrationBuilder.DropTable(
                name: "EscalationTiers");

            migrationBuilder.DropTable(
                name: "EscalationPolicies");

            migrationBuilder.DropIndex(
                name: "IX_Services_EscalationPolicyId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "EscalationPolicyId",
                table: "Services");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Services",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Services_ServiceId",
                table: "Incidents",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

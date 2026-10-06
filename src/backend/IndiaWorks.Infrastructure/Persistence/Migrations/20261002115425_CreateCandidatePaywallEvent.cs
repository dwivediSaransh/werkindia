using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateCandidatePaywallEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidatePaywallEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    JobContext = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FreeLimit = table.Column<int>(type: "int", nullable: false),
                    FreeUsed = table.Column<int>(type: "int", nullable: false),
                    FreeRemaining = table.Column<int>(type: "int", nullable: false),
                    AccessSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidatePaywallEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidatePaywallEvents_CorrelationId",
                table: "CandidatePaywallEvents",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidatePaywallEvents_OrgId_CandidateProfileId_CreatedAt",
                table: "CandidatePaywallEvents",
                columns: new[] { "OrganisationId", "CandidateProfileId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidatePaywallEvents");
        }
    }
}

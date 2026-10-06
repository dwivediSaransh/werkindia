using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateOrganisationCommercialAccessState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganisationCommercialAccessStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FreeCandidateUnlockLimit = table.Column<int>(type: "int", nullable: false),
                    FreeCandidateUnlockUsed = table.Column<int>(type: "int", nullable: false),
                    ProvisioningState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResetCadence = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationCommercialAccessStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCommercialAccessStates_OrganisationId",
                table: "OrganisationCommercialAccessStates",
                column: "OrganisationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganisationCommercialAccessStates");
        }
    }
}

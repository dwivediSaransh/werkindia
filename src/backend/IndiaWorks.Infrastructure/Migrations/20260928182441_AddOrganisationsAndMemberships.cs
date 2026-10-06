using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganisationsAndMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OrganisationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmployeeBand = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Nic2025Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationIdentifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentifierType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdentifierValueEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IdentifierHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    MaskedDisplay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VerificationSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationIdentifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganisationIdentifiers_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationMemberships",
                columns: table => new
                {
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationMemberships", x => new { x.OrganisationId, x.UserAccountId });
                    table.ForeignKey(
                        name: "FK_OrganisationMemberships_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganisationMemberships_UserAccounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationIdentifiers_OrganisationId",
                table: "OrganisationIdentifiers",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "UQ_OrganisationIdentifiers_Type_Hash",
                table: "OrganisationIdentifiers",
                columns: new[] { "IdentifierType", "IdentifierHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationMemberships_OrganisationId_Status",
                table: "OrganisationMemberships",
                columns: new[] { "OrganisationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationMemberships_UserAccountId",
                table: "OrganisationMemberships",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_LegalName",
                table: "Organisations",
                column: "LegalName");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_VerificationStatus",
                table: "Organisations",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganisationIdentifiers");

            migrationBuilder.DropTable(
                name: "OrganisationMemberships");

            migrationBuilder.DropTable(
                name: "Organisations");
        }
    }
}

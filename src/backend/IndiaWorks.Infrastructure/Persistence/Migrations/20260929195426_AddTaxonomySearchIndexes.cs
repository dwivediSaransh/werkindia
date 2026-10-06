using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxonomySearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkContexts_Name",
                table: "WorkContexts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Specialisations_Name",
                table: "Specialisations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CanonicalName",
                table: "Roles",
                column: "CanonicalName");

            migrationBuilder.CreateIndex(
                name: "IX_JobFamilies_Name",
                table: "JobFamilies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CredentialTypes_Name",
                table: "CredentialTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_Name",
                table: "AssetTypes",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkContexts_Name",
                table: "WorkContexts");

            migrationBuilder.DropIndex(
                name: "IX_Specialisations_Name",
                table: "Specialisations");

            migrationBuilder.DropIndex(
                name: "IX_Skills_Name",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Roles_CanonicalName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_JobFamilies_Name",
                table: "JobFamilies");

            migrationBuilder.DropIndex(
                name: "IX_CredentialTypes_Name",
                table: "CredentialTypes");

            migrationBuilder.DropIndex(
                name: "IX_AssetTypes_Name",
                table: "AssetTypes");
        }
    }
}

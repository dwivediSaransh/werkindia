using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganisationCommercialIdentityFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "CommercialIdentityFingerprint",
                table: "Organisations",
                type: "varbinary(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommercialIdentityFingerprint",
                table: "Organisations");
        }
    }
}

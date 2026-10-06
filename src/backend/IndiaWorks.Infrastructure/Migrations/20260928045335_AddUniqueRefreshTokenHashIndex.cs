using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueRefreshTokenHashIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_UserDevices_RefreshTokenHash",
                table: "UserDevices",
                column: "RefreshTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_UserDevices_RefreshTokenHash",
                table: "UserDevices");
        }
    }
}

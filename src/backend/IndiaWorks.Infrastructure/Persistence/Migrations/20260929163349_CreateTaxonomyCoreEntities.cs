using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateTaxonomyCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxonomyVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemanticVersion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Status = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobFamilies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    TaxonomyVersionId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobFamilies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobFamilies_JobFamilies_ParentId",
                        column: x => x.ParentId,
                        principalTable: "JobFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobFamilies_TaxonomyVersions_TaxonomyVersionId",
                        column: x => x.TaxonomyVersionId,
                        principalTable: "TaxonomyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobFamilyId = table.Column<long>(type: "bigint", nullable: false),
                    TaxonomyVersionId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalTable: "JobFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Roles_TaxonomyVersions_TaxonomyVersionId",
                        column: x => x.TaxonomyVersionId,
                        principalTable: "TaxonomyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RegionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AliasType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAliases_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Specialisations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialisations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specialisations_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Specialisations_Specialisations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Specialisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkContexts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkContexts_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobFamilies_ParentId",
                table: "JobFamilies",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JobFamilies_TaxonomyVersionId",
                table: "JobFamilies",
                column: "TaxonomyVersionId");

            migrationBuilder.CreateIndex(
                name: "UQ_JobFamilies_TaxVersion_Code",
                table: "JobFamilies",
                columns: new[] { "TaxonomyVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAliases_Alias",
                table: "RoleAliases",
                column: "Alias")
                .Annotation("SqlServer:Include", new[] { "RoleId", "Weight" });

            migrationBuilder.CreateIndex(
                name: "UQ_RoleAliases_Role_Alias",
                table: "RoleAliases",
                columns: new[] { "RoleId", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_JobFamilyId",
                table: "Roles",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TaxonomyVersionId",
                table: "Roles",
                column: "TaxonomyVersionId");

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_TaxVersion_Slug",
                table: "Roles",
                columns: new[] { "TaxonomyVersionId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialisations_ParentId",
                table: "Specialisations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialisations_RoleId",
                table: "Specialisations",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ_Specialisations_Role_Code",
                table: "Specialisations",
                columns: new[] { "RoleId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyVersions_Status",
                table: "TaxonomyVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UQ_TaxonomyVersions_SemanticVersion",
                table: "TaxonomyVersions",
                column: "SemanticVersion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkContexts_RoleId",
                table: "WorkContexts",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ_WorkContexts_Role_Code",
                table: "WorkContexts",
                columns: new[] { "RoleId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleAliases");

            migrationBuilder.DropTable(
                name: "Specialisations");

            migrationBuilder.DropTable(
                name: "WorkContexts");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "JobFamilies");

            migrationBuilder.DropTable(
                name: "TaxonomyVersions");
        }
    }
}

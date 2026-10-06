using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaWorks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingWebhookInboxAndPaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingWebhookInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderEventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SignatureVerified = table.Column<bool>(type: "bit", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderOccurredAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ProcessStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Received"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookInbox_ProcessStatus",
                table: "BillingWebhookInbox",
                column: "ProcessStatus");

            migrationBuilder.CreateIndex(
                name: "UQ_BillingWebhookInbox_Provider_ProviderEventId",
                table: "BillingWebhookInbox",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_BillingInvoiceId",
                table: "PaymentTransactions",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrganisationId",
                table: "PaymentTransactions",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "UQ_PaymentTransactions_Provider_ProviderTransactionId",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "ProviderTransactionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingWebhookInbox");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");
        }
    }
}

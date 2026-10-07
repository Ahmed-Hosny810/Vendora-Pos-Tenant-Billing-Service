using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.tenant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class newSubscriptionInvoiceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoices_TenantId_TenantSubscriptionId_DueDate",
                table: "SubscriptionInvoices",
                columns: new[] { "TenantId", "TenantSubscriptionId", "DueDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionInvoices_TenantId_TenantSubscriptionId_DueDate",
                table: "SubscriptionInvoices");
        }
    }
}

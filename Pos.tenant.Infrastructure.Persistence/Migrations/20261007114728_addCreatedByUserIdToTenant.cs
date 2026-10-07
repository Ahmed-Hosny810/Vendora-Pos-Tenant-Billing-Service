using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.tenant.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addCreatedByUserIdToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CreatedByUserId",
                table: "Tenants",
                column: "CreatedByUserId",
                unique: true,
                filter: "[CreatedByUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_CreatedByUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Tenants");
        }
    }
}

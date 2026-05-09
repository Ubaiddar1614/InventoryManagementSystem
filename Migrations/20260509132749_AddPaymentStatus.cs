using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This is the ONLY thing that should be in the Up method!
            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // If we ever need to undo this, it just deletes the column
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Transactions");
        }
    }
}
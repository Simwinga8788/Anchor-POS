using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurfPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "TransactionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "TransactionItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "StockLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "StockLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Shifts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Shifts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "AppSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "AppSettings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "StockLogs");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "StockLogs");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "AppSettings");
        }
    }
}

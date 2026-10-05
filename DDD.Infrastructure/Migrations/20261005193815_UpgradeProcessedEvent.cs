using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeProcessedEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ProcessedOnUtc",
                table: "ProcessedEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ProcessedEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ClaimToken",
                table: "ProcessedEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedUntilUtc",
                table: "ProcessedEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                table: "ProcessedEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "ProcessedEvents",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOnUtc",
                table: "ProcessedEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_ClaimToken",
                table: "ProcessedEvents",
                column: "ClaimToken");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_ProcessedOnUtc_ClaimedUntilUtc",
                table: "ProcessedEvents",
                columns: new[] { "ProcessedOnUtc", "ClaimedUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedEvents_ClaimToken",
                table: "ProcessedEvents");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedEvents_ProcessedOnUtc_ClaimedUntilUtc",
                table: "ProcessedEvents");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ProcessedEvents");

            migrationBuilder.DropColumn(
                name: "ClaimToken",
                table: "ProcessedEvents");

            migrationBuilder.DropColumn(
                name: "ClaimedUntilUtc",
                table: "ProcessedEvents");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                table: "ProcessedEvents");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "ProcessedEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedOnUtc",
                table: "ProcessedEvents");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ProcessedOnUtc",
                table: "ProcessedEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}

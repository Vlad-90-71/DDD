using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DDD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameProcessedEventToEventProcessingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLogs");

            migrationBuilder.RenameTable(
                name: "ProcessedEvents",
                newName: "EventProcessingStates");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessedEvents_ClaimToken",
                table: "EventProcessingStates",
                newName: "IX_EventProcessingStates_ClaimToken");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessedEvents_ProcessedOnUtc_ClaimedUntilUtc",
                table: "EventProcessingStates",
                newName: "IX_EventProcessingStates_ProcessedOnUtc_ClaimedUntilUtc");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLogs");

            migrationBuilder.RenameTable(
                name: "EventProcessingStates",
                newName: "ProcessedEvents");

            migrationBuilder.RenameIndex(
                name: "IX_EventProcessingStates_ClaimToken",
                table: "ProcessedEvents",
                newName: "IX_ProcessedEvents_ClaimToken");

            migrationBuilder.RenameIndex(
                name: "IX_EventProcessingStates_ProcessedOnUtc_ClaimedUntilUtc",
                table: "ProcessedEvents",
                newName: "IX_ProcessedEvents_ProcessedOnUtc_ClaimedUntilUtc");
        }
    }
}

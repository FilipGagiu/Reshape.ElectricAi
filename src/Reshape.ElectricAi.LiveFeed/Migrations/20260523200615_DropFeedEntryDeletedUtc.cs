using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.LiveFeed.Migrations
{
    /// <inheritdoc />
    public partial class DropFeedEntryDeletedUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_feed_entries_DeletedUtc_PublishedUtc",
                schema: "feed",
                table: "feed_entries");

            migrationBuilder.DropColumn(
                name: "DeletedUtc",
                schema: "feed",
                table: "feed_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedUtc",
                schema: "feed",
                table: "feed_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_feed_entries_DeletedUtc_PublishedUtc",
                schema: "feed",
                table: "feed_entries",
                columns: new[] { "DeletedUtc", "PublishedUtc" },
                descending: new[] { false, true },
                filter: "\"DeletedUtc\" IS NULL");
        }
    }
}

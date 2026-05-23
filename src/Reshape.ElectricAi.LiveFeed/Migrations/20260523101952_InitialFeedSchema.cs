using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.LiveFeed.Migrations
{
    /// <inheritdoc />
    public partial class InitialFeedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "feed");

            migrationBuilder.CreateTable(
                name: "feed_entries",
                schema: "feed",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PrimaryCategory = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsGeneral = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feed_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "feed_entry_artists",
                schema: "feed",
                columns: table => new
                {
                    FeedEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feed_entry_artists", x => new { x.FeedEntryId, x.ArtistName });
                    table.ForeignKey(
                        name: "FK_feed_entry_artists_feed_entries_FeedEntryId",
                        column: x => x.FeedEntryId,
                        principalSchema: "feed",
                        principalTable: "feed_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feed_entry_genres",
                schema: "feed",
                columns: table => new
                {
                    FeedEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Genre = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feed_entry_genres", x => new { x.FeedEntryId, x.Genre });
                    table.ForeignKey(
                        name: "FK_feed_entry_genres_feed_entries_FeedEntryId",
                        column: x => x.FeedEntryId,
                        principalSchema: "feed",
                        principalTable: "feed_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_feed_entries_DeletedUtc_PublishedUtc",
                schema: "feed",
                table: "feed_entries",
                columns: new[] { "DeletedUtc", "PublishedUtc" },
                descending: new[] { false, true },
                filter: "\"DeletedUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_feed_entries_PublishedUtc",
                schema: "feed",
                table: "feed_entries",
                column: "PublishedUtc",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feed_entry_artists",
                schema: "feed");

            migrationBuilder.DropTable(
                name: "feed_entry_genres",
                schema: "feed");

            migrationBuilder.DropTable(
                name: "feed_entries",
                schema: "feed");
        }
    }
}

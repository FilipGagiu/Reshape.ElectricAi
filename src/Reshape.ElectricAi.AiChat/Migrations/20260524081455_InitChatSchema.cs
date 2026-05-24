using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.AiChat.Migrations
{
    /// <inheritdoc />
    public partial class InitChatSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserMessageCount = table.Column<int>(type: "integer", nullable: false),
                    IsGenerating = table.Column<bool>(type: "boolean", nullable: false),
                    GeneratingStartedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversation_messages",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<byte>(type: "smallint", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversation_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "chat",
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_ConversationId_OrderIndex",
                schema: "chat",
                table: "conversation_messages",
                columns: new[] { "ConversationId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_UserId_LastMessageUtc",
                schema: "chat",
                table: "conversations",
                columns: new[] { "UserId", "LastMessageUtc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_messages",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "chat");
        }
    }
}

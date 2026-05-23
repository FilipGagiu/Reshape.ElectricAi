using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.Plans.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferenceCuisines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferenceCuisines",
                schema: "plans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cuisine = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferenceCuisines", x => new { x.UserId, x.Cuisine });
                    table.ForeignKey(
                        name: "FK_UserPreferenceCuisines_UserPreferences_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "UserPreferences",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferenceCuisines",
                schema: "plans");
        }
    }
}

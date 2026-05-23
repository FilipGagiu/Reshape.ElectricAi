using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.Plans.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupPreferenceCuisines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupPreferenceCuisines",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cuisine = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPreferenceCuisines", x => new { x.GroupId, x.Cuisine });
                    table.ForeignKey(
                        name: "FK_GroupPreferenceCuisines_GroupPreferences_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "GroupPreferences",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupPreferenceCuisines",
                schema: "plans");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.Plans.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanTip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tip",
                schema: "plans",
                table: "Plans",
                type: "text",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tip",
                schema: "plans",
                table: "Plans");
        }
    }
}

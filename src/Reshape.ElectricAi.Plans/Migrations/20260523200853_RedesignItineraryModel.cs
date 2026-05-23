using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.Plans.Migrations
{
    /// <inheritdoc />
    public partial class RedesignItineraryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipe existing snapshots before destructive schema change.
            // Old ContentJson shape (PlanDto with days/food/budget) is incompatible with
            // the new ItineraryDto shape; rows would also collide with the new unique
            // index on OwnerUserId once GroupId-only rows have their OwnerUserId
            // defaulted to Guid.Empty. Dev DB only — no prod data exists yet.
            migrationBuilder.Sql("DELETE FROM plans.\"Plans\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Plans_Groups_GroupId",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceActivities",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceArtists",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceCuisines",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceFoodRestrictions",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceGenres",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferences",
                schema: "plans");

            migrationBuilder.DropIndex(
                name: "IX_Plans_GroupId",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Plans_OwnerUserId",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_plans_owner_xor_group",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ExportedUtc",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Scope",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "TicketType",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Tip",
                schema: "plans",
                table: "Plans");

            migrationBuilder.AddColumn<string>(
                name: "AccommodationNote",
                schema: "plans",
                table: "UserPreferences",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "CrewEstimatedSize",
                schema: "plans",
                table: "UserPreferences",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CrewKind",
                schema: "plans",
                table: "UserPreferences",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "plans",
                table: "UserPreferences",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                schema: "plans",
                table: "UserPreferences",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportNote",
                schema: "plans",
                table: "UserPreferences",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                schema: "plans",
                table: "Plans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "UserPreferenceVibeTags",
                schema: "plans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferenceVibeTags", x => new { x.UserId, x.Value });
                    table.ForeignKey(
                        name: "FK_UserPreferenceVibeTags_UserPreferences_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "UserPreferences",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_OwnerUserId",
                schema: "plans",
                table: "Plans",
                column: "OwnerUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferenceVibeTags",
                schema: "plans");

            migrationBuilder.DropIndex(
                name: "IX_Plans_OwnerUserId",
                schema: "plans",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "AccommodationNote",
                schema: "plans",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "CrewEstimatedSize",
                schema: "plans",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "CrewKind",
                schema: "plans",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "plans",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "Origin",
                schema: "plans",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "TransportNote",
                schema: "plans",
                table: "UserPreferences");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                schema: "plans",
                table: "Plans",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedUtc",
                schema: "plans",
                table: "Plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "plans",
                table: "Plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                schema: "plans",
                table: "Plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketType",
                schema: "plans",
                table: "Plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tip",
                schema: "plans",
                table: "Plans",
                type: "text",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupPreferences",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Accommodation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AgeGroup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TicketType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Transport = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPreferences", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_GroupPreferences_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupPreferenceActivities",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Activity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPreferenceActivities", x => new { x.GroupId, x.Activity });
                    table.ForeignKey(
                        name: "FK_GroupPreferenceActivities_GroupPreferences_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "GroupPreferences",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupPreferenceArtists",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPreferenceArtists", x => new { x.GroupId, x.ArtistName });
                    table.ForeignKey(
                        name: "FK_GroupPreferenceArtists_GroupPreferences_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "GroupPreferences",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "GroupPreferenceFoodRestrictions",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Restriction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPreferenceFoodRestrictions", x => new { x.GroupId, x.Restriction });
                    table.ForeignKey(
                        name: "FK_GroupPreferenceFoodRestrictions_GroupPreferences_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "GroupPreferences",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupPreferenceGenres",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Genre = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPreferenceGenres", x => new { x.GroupId, x.Genre });
                    table.ForeignKey(
                        name: "FK_GroupPreferenceGenres_GroupPreferences_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "GroupPreferences",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_GroupId",
                schema: "plans",
                table: "Plans",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_OwnerUserId",
                schema: "plans",
                table: "Plans",
                column: "OwnerUserId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_plans_owner_xor_group",
                schema: "plans",
                table: "Plans",
                sql: "(\"OwnerUserId\" IS NULL) <> (\"GroupId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Plans_Groups_GroupId",
                schema: "plans",
                table: "Plans",
                column: "GroupId",
                principalSchema: "plans",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

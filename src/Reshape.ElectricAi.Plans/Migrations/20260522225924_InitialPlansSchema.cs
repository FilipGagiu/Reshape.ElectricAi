using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshape.ElectricAi.Plans.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlansSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "plans");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "plans",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(88)", maxLength: 88, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByHash = table.Column<string>(type: "character varying(88)", maxLength: 88, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                schema: "plans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Accommodation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Transport = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AgeGroup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupPreferences",
                schema: "plans",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Accommodation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Transport = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AgeGroup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                name: "Plans",
                schema: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExportedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.CheckConstraint("ck_plans_owner_xor_group", "(\"OwnerUserId\" IS NULL) <> (\"GroupId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_Plans_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "plans",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Plans_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "plans",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferenceActivities",
                schema: "plans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Activity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferenceActivities", x => new { x.UserId, x.Activity });
                    table.ForeignKey(
                        name: "FK_UserPreferenceActivities_UserPreferences_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "UserPreferences",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferenceArtists",
                schema: "plans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferenceArtists", x => new { x.UserId, x.ArtistName });
                    table.ForeignKey(
                        name: "FK_UserPreferenceArtists_UserPreferences_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "UserPreferences",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferenceFoodRestrictions",
                schema: "plans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Restriction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferenceFoodRestrictions", x => new { x.UserId, x.Restriction });
                    table.ForeignKey(
                        name: "FK_UserPreferenceFoodRestrictions_UserPreferences_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "UserPreferences",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferenceGenres",
                schema: "plans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Genre = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferenceGenres", x => new { x.UserId, x.Genre });
                    table.ForeignKey(
                        name: "FK_UserPreferenceGenres_UserPreferences_UserId",
                        column: x => x.UserId,
                        principalSchema: "plans",
                        principalTable: "UserPreferences",
                        principalColumn: "UserId",
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
                name: "IX_GroupMembers_UserId",
                schema: "plans",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OwnerUserId",
                schema: "plans",
                table: "Groups",
                column: "OwnerUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "plans",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "plans",
                table: "RefreshTokens",
                column: "UserId",
                filter: "\"RevokedUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "plans",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupMembers",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceActivities",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceArtists",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceFoodRestrictions",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferenceGenres",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "UserPreferenceActivities",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "UserPreferenceArtists",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "UserPreferenceFoodRestrictions",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "UserPreferenceGenres",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "GroupPreferences",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "UserPreferences",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "plans");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "plans");
        }
    }
}

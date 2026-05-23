using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Reshape.ElectricAi.VectorDb.Migrations
{
    /// <inheritdoc />
    public partial class InitVectorDb : Migration
    {
        private static readonly string[] CosineOps = ["vector_cosine_ops"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vector");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "vector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IngestedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_entries",
                schema: "vector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TextRepresentation = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    CategoryTags = table.Column<string[]>(type: "text[]", nullable: false),
                    EventUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IngestedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                schema: "vector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    TextHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    CategoryTags = table.Column<string[]>(type: "text[]", nullable: false),
                    IngestedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "document_chunks",
                schema: "vector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    CategoryTags = table.Column<string[]>(type: "text[]", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_chunks_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "vector",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "answers",
                schema: "vector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    CategoryTags = table.Column<string[]>(type: "text[]", nullable: false),
                    IngestedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_answers_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "vector",
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_answers_CategoryTags",
                schema: "vector",
                table: "answers",
                column: "CategoryTags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_answers_Embedding",
                schema: "vector",
                table: "answers",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", CosineOps);

            migrationBuilder.CreateIndex(
                name: "IX_answers_QuestionId",
                schema: "vector",
                table: "answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_CategoryTags",
                schema: "vector",
                table: "document_chunks",
                column: "CategoryTags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId",
                schema: "vector",
                table: "document_chunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_Embedding",
                schema: "vector",
                table: "document_chunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", CosineOps);

            migrationBuilder.CreateIndex(
                name: "IX_documents_SourceHash",
                schema: "vector",
                table: "documents",
                column: "SourceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_entries_CategoryTags",
                schema: "vector",
                table: "event_entries",
                column: "CategoryTags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_event_entries_Embedding",
                schema: "vector",
                table: "event_entries",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", CosineOps);

            migrationBuilder.CreateIndex(
                name: "IX_event_entries_FeedEntryId",
                schema: "vector",
                table: "event_entries",
                column: "FeedEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_questions_CategoryTags",
                schema: "vector",
                table: "questions",
                column: "CategoryTags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_questions_Embedding",
                schema: "vector",
                table: "questions",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", CosineOps);

            migrationBuilder.CreateIndex(
                name: "IX_questions_TextHash",
                schema: "vector",
                table: "questions",
                column: "TextHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answers",
                schema: "vector");

            migrationBuilder.DropTable(
                name: "document_chunks",
                schema: "vector");

            migrationBuilder.DropTable(
                name: "event_entries",
                schema: "vector");

            migrationBuilder.DropTable(
                name: "questions",
                schema: "vector");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "vector");
        }
    }
}

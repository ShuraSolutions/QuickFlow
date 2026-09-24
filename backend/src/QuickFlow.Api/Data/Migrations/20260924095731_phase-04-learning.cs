using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class phase04learning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LearningCardId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningMilestones_LearningCards_LearningCardId",
                        column: x => x.LearningCardId,
                        principalTable: "LearningCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LearningCardId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningNotes_LearningCards_LearningCardId",
                        column: x => x.LearningCardId,
                        principalTable: "LearningCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningCards_Status",
                table: "LearningCards",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LearningMilestones_LearningCardId",
                table: "LearningMilestones",
                column: "LearningCardId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningNotes_LearningCardId",
                table: "LearningNotes",
                column: "LearningCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningMilestones");

            migrationBuilder.DropTable(
                name: "LearningNotes");

            migrationBuilder.DropTable(
                name: "LearningCards");
        }
    }
}

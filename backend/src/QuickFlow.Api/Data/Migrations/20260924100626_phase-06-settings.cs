using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class phase06settings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PlanStartNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationLeadMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultView = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "UserSettings",
                columns: new[] { "Id", "DefaultView", "DisplayName", "Email", "NotificationLeadMinutes", "PlanStartNotificationsEnabled", "Theme", "UpdatedAt" },
                values: new object[] { 1, "Dashboard", "QuickFlow User", null, 0, true, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}

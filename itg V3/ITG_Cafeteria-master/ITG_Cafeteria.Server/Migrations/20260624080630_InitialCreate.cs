using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITG_Cafeteria.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CafeteriaUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyMenus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyMenus_CafeteriaUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "CafeteriaUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeeklyMenuId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuDays_WeeklyMenus_WeeklyMenuId",
                        column: x => x.WeeklyMenuId,
                        principalTable: "WeeklyMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuDayId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuNodes_MenuDays_MenuDayId",
                        column: x => x.MenuDayId,
                        principalTable: "MenuDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuNodes_MenuNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CafeteriaUsers_Email",
                table: "CafeteriaUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuDays_WeeklyMenuId_DayOfWeek",
                table: "MenuDays",
                columns: new[] { "WeeklyMenuId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuNodes_MenuDayId_ParentId_SortOrder",
                table: "MenuNodes",
                columns: new[] { "MenuDayId", "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuNodes_ParentId",
                table: "MenuNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyMenus_CreatedByUserId",
                table: "WeeklyMenus",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyMenus_WeekStartDate",
                table: "WeeklyMenus",
                column: "WeekStartDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuNodes");

            migrationBuilder.DropTable(
                name: "MenuDays");

            migrationBuilder.DropTable(
                name: "WeeklyMenus");

            migrationBuilder.DropTable(
                name: "CafeteriaUsers");
        }
    }
}

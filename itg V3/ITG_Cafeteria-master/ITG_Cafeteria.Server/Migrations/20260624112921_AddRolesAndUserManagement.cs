using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITG_Cafeteria.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CafeteriaUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "CafeteriaUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_CafeteriaUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "CafeteriaUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CafeteriaUsers_Username",
                table: "CafeteriaUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.Sql(@"
                UPDATE CafeteriaUsers
                SET Username = LEFT(Email, CHARINDEX('@', Email) - 1)
                WHERE Username = '' AND CHARINDEX('@', Email) > 0;

                UPDATE CafeteriaUsers
                SET Username = Email
                WHERE Username = '';
            ");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT Roles ON;
                INSERT INTO Roles (Id, Name) VALUES
                    (1, 'SystemAdmin'),
                    (2, 'CafeteriaEditor'),
                    (3, 'CafeteriaPublisher');
                SET IDENTITY_INSERT Roles OFF;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM CafeteriaUsers WHERE Username = 'SystemAdmin')
                INSERT INTO CafeteriaUsers (Username, Email, PasswordHash, DisplayName, IsActive, CreatedAt)
                VALUES (
                    'SystemAdmin',
                    'systemadmin@itg.local',
                    '$2a$11$5J2ZQbjSB6TFY3M.b9bopu6TIOD2aOENUZI42CwvpE9blMcVFHA/u',
                    'System Administrator',
                    1,
                    GETUTCDATE()
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO UserRoles (UserId, RoleId)
                SELECT u.Id, r.Id
                FROM CafeteriaUsers u
                CROSS JOIN Roles r
                WHERE u.Username = 'SystemAdmin' AND r.Name = 'SystemAdmin'
                AND NOT EXISTS (
                    SELECT 1 FROM UserRoles ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_CafeteriaUsers_Username",
                table: "CafeteriaUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CafeteriaUsers");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "CafeteriaUsers");
        }
    }
}

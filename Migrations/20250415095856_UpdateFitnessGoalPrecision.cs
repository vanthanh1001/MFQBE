using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessApp.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFitnessGoalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_FirebaseUid",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "UserChallenges");

            migrationBuilder.RenameColumn(
                name: "JoinedAt",
                table: "UserChallenges",
                newName: "JoinDate");

            migrationBuilder.AlterColumn<string>(
                name: "FirebaseUid",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserChallenges",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserChallenges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
                table: "Challenges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Meals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Calories = table.Column<int>(type: "int", nullable: false),
                    Ingredients = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirebaseUid",
                table: "Users",
                column: "FirebaseUid",
                unique: true,
                filter: "[FirebaseUid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_UserId",
                table: "Meals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meals");

            migrationBuilder.DropIndex(
                name: "IX_Users_FirebaseUid",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserChallenges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserChallenges");

            migrationBuilder.DropColumn(
                name: "RewardPoints",
                table: "Challenges");

            migrationBuilder.RenameColumn(
                name: "JoinDate",
                table: "UserChallenges",
                newName: "JoinedAt");

            migrationBuilder.AlterColumn<string>(
                name: "FirebaseUid",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "UserChallenges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirebaseUid",
                table: "Users",
                column: "FirebaseUid",
                unique: true);
        }
    }
}

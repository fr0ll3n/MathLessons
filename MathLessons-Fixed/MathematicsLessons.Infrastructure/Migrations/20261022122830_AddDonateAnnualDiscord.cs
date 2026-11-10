using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathematicsLessons.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonateAnnualDiscord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResourceUrl",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Exercises");

            migrationBuilder.RenameColumn(
                name: "MeetingLink",
                table: "Schedules",
                newName: "DiscordInviteUrl");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerYear",
                table: "Subs",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscordChannel",
                table: "Schedules",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Receipts",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Question",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Donations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DonorName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Donations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Donations_UserId",
                table: "Donations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Donations");

            migrationBuilder.DropColumn(
                name: "PricePerYear",
                table: "Subs");

            migrationBuilder.DropColumn(
                name: "DiscordChannel",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "DiscordInviteUrl",
                table: "Schedules",
                newName: "MeetingLink");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Receipts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "ResourceUrl",
                table: "Lessons",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Question",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Exercises",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

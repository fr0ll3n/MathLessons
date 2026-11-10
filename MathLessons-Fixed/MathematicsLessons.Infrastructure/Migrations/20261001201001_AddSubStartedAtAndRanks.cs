using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathematicsLessons.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubStartedAtAndRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubStartedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubStartedAt",
                table: "AspNetUsers");
        }
    }
}

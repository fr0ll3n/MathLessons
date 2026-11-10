using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathematicsLessons.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingLinkAndSessionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "Appointments",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "Appointments");
        }
    }
}

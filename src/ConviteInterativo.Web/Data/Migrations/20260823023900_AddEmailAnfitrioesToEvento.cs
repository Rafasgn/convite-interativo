using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConviteInterativo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAnfitrioesToEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailAnfitrioes",
                table: "Eventos",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "admin@example.com");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailAnfitrioes",
                table: "Eventos");
        }
    }
}

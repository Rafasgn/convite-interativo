using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConviteInterativo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnfitrioesHomenageadoToEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Anfitrioes",
                table: "Eventos",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Homenageado",
                table: "Eventos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anfitrioes",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "Homenageado",
                table: "Eventos");
        }
    }
}

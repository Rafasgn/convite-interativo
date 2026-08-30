using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConviteInterativo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiasConfirmacaoToEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasConfirmacao",
                table: "Eventos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasConfirmacao",
                table: "Eventos");
        }
    }
}

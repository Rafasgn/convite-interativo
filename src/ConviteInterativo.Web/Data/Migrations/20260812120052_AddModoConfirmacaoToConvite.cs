using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConviteInterativo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModoConfirmacaoToConvite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModoConfirmacao",
                table: "Convites",
                type: "TEXT",
                nullable: false,
                defaultValue: "Grupo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModoConfirmacao",
                table: "Convites");
        }
    }
}

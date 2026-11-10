using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenaApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AvatarDeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Avatar",
                table: "Usuarios",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarTipo",
                table: "Usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AvatarTipo",
                table: "Usuarios");
        }
    }
}

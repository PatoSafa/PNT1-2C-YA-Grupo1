using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenaApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SuperAdminYUsuarioActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Corregido a mano: EF Core propone false (el valor por defecto de bool) y eso dejaría
            // desactivados a todos los usuarios que ya existen. Ver E-07 en docs/ERRORES.md.
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Usuarios");
        }
    }
}

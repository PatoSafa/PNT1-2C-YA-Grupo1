using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenaApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImagenDeIngrediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Imagen",
                table: "Ingredientes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenTipo",
                table: "Ingredientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Imagen",
                table: "Ingredientes");

            migrationBuilder.DropColumn(
                name: "ImagenTipo",
                table: "Ingredientes");
        }
    }
}

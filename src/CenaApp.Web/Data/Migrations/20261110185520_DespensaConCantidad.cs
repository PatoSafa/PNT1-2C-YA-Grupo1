using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenaApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class DespensaConCantidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Despensa",
                table: "Despensa");

            migrationBuilder.DropIndex(
                name: "IX_Despensa_UsuarioId",
                table: "Despensa");

            migrationBuilder.AddColumn<decimal>(
                name: "Cantidad",
                table: "Despensa",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unidad",
                table: "Despensa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Despensa",
                table: "Despensa",
                columns: new[] { "UsuarioId", "IngredienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Despensa_IngredienteId",
                table: "Despensa",
                column: "IngredienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Despensa",
                table: "Despensa");

            migrationBuilder.DropIndex(
                name: "IX_Despensa_IngredienteId",
                table: "Despensa");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "Despensa");

            migrationBuilder.DropColumn(
                name: "Unidad",
                table: "Despensa");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Despensa",
                table: "Despensa",
                columns: new[] { "IngredienteId", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_Despensa_UsuarioId",
                table: "Despensa",
                column: "UsuarioId");
        }
    }
}

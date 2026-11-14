using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class indicesUnicosCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subcategorias_IdCategoria",
                table: "Subcategorias");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategorias_IdCategoria_Nombre",
                table: "Subcategorias",
                columns: new[] { "IdCategoria", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposMusculares_Nombre",
                table: "GruposMusculares",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disciplinas_Nombre",
                table: "Disciplinas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subcategorias_IdCategoria_Nombre",
                table: "Subcategorias");

            migrationBuilder.DropIndex(
                name: "IX_GruposMusculares_Nombre",
                table: "GruposMusculares");

            migrationBuilder.DropIndex(
                name: "IX_Disciplinas_Nombre",
                table: "Disciplinas");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategorias_IdCategoria",
                table: "Subcategorias",
                column: "IdCategoria");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class ResenaPlataformaUnica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResenasPlataforma_IdUsuario",
                table: "ResenasPlataforma");

            migrationBuilder.CreateIndex(
                name: "IX_ResenasPlataforma_IdUsuario",
                table: "ResenasPlataforma",
                column: "IdUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResenasPlataforma_IdUsuario",
                table: "ResenasPlataforma");

            migrationBuilder.CreateIndex(
                name: "IX_ResenasPlataforma_IdUsuario",
                table: "ResenasPlataforma",
                column: "IdUsuario");
        }
    }
}

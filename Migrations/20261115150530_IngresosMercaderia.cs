using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class IngresosMercaderia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngresosMercaderia",
                columns: table => new
                {
                    IdIngreso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NumeroComprobante = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Anulado = table.Column<bool>(type: "bit", nullable: false),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdUsuario = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngresosMercaderia", x => x.IdIngreso);
                    table.ForeignKey(
                        name: "FK_IngresosMercaderia_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesIngreso",
                columns: table => new
                {
                    IdDetalleIngreso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IdIngreso = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesIngreso", x => x.IdDetalleIngreso);
                    table.CheckConstraint("CK_DetallesIngreso_Cantidad", "[Cantidad] > 0");
                    table.CheckConstraint("CK_DetallesIngreso_Costo", "[CostoUnitario] >= 0");
                    table.ForeignKey(
                        name: "FK_DetallesIngreso_IngresosMercaderia_IdIngreso",
                        column: x => x.IdIngreso,
                        principalTable: "IngresosMercaderia",
                        principalColumn: "IdIngreso",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesIngreso_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesIngreso_IdIngreso_IdProducto",
                table: "DetallesIngreso",
                columns: new[] { "IdIngreso", "IdProducto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesIngreso_IdProducto",
                table: "DetallesIngreso",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_IngresosMercaderia_Fecha",
                table: "IngresosMercaderia",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_IngresosMercaderia_IdUsuario",
                table: "IngresosMercaderia",
                column: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesIngreso");

            migrationBuilder.DropTable(
                name: "IngresosMercaderia");
        }
    }
}

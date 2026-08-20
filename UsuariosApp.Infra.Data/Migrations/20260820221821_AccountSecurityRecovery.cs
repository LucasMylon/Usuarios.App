using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsuariosApp.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountSecurityRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EMAIL_CONFIRMATION_EXPIRES_AT_UTC",
                table: "USUARIOS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TELEFONE",
                table: "USUARIOS",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TELEFONE_CONFIRMADO",
                table: "USUARIOS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VERSAO_SEGURANCA",
                table: "USUARIOS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "USUARIO_TOKENS",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USUARIO_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TIPO = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TOKEN_HASH = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DESTINO = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CRIADO_EM_UTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EXPIRA_EM_UTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CONSUMIDO_EM_UTC = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TENTATIVAS = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO_TOKENS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_USUARIO_TOKENS_USUARIOS_USUARIO_ID",
                        column: x => x.USUARIO_ID,
                        principalTable: "USUARIOS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USUARIOS_TELEFONE",
                table: "USUARIOS",
                column: "TELEFONE",
                unique: true,
                filter: "[TELEFONE] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_TOKENS_TIPO_TOKEN_HASH",
                table: "USUARIO_TOKENS",
                columns: new[] { "TIPO", "TOKEN_HASH" });

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_TOKENS_USUARIO_ID_TIPO_EXPIRA_EM_UTC",
                table: "USUARIO_TOKENS",
                columns: new[] { "USUARIO_ID", "TIPO", "EXPIRA_EM_UTC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USUARIO_TOKENS");

            migrationBuilder.DropIndex(
                name: "IX_USUARIOS_TELEFONE",
                table: "USUARIOS");

            migrationBuilder.DropColumn(
                name: "EMAIL_CONFIRMATION_EXPIRES_AT_UTC",
                table: "USUARIOS");

            migrationBuilder.DropColumn(
                name: "TELEFONE",
                table: "USUARIOS");

            migrationBuilder.DropColumn(
                name: "TELEFONE_CONFIRMADO",
                table: "USUARIOS");

            migrationBuilder.DropColumn(
                name: "VERSAO_SEGURANCA",
                table: "USUARIOS");
        }
    }
}

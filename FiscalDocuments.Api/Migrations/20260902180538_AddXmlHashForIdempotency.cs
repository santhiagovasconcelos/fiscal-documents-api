using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscalDocuments.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddXmlHashForIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XmlHash",
                table: "FiscalDocuments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_XmlHash",
                table: "FiscalDocuments",
                column: "XmlHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_XmlHash",
                table: "FiscalDocuments");

            migrationBuilder.DropColumn(
                name: "XmlHash",
                table: "FiscalDocuments");
        }
    }
}

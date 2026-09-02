using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscalDocuments.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveToFiscalDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "FiscalDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "FiscalDocuments");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogLens.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceNameAndTraceIdToLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "logs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "logs");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "logs");
        }
    }
}

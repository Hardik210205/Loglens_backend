using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiTenancy_And_JSONB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "TenantId", table: "users", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "TenantId", table: "services", type: "uuid", nullable: true);
            
            migrationBuilder.Sql(@"ALTER TABLE logs ALTER COLUMN ""Metadata"" TYPE jsonb USING ""Metadata""::jsonb;");

            migrationBuilder.AddColumn<Guid>(name: "TenantId", table: "logs", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "TenantId", table: "api_keys", type: "uuid", nullable: true);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_users_TenantId", table: "users", column: "TenantId");
            migrationBuilder.CreateIndex(name: "IX_services_TenantId", table: "services", column: "TenantId");
            migrationBuilder.CreateIndex(name: "IX_logs_TenantId", table: "logs", column: "TenantId");
            migrationBuilder.CreateIndex(name: "IX_api_keys_TenantId", table: "api_keys", column: "TenantId");

            migrationBuilder.AddForeignKey(name: "FK_api_keys_Tenants_TenantId", table: "api_keys", column: "TenantId", principalTable: "Tenants", principalColumn: "Id");
            migrationBuilder.AddForeignKey(name: "FK_logs_Tenants_TenantId", table: "logs", column: "TenantId", principalTable: "Tenants", principalColumn: "Id");
            migrationBuilder.AddForeignKey(name: "FK_services_Tenants_TenantId", table: "services", column: "TenantId", principalTable: "Tenants", principalColumn: "Id");
            migrationBuilder.AddForeignKey(name: "FK_users_Tenants_TenantId", table: "users", column: "TenantId", principalTable: "Tenants", principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_keys_Tenants_TenantId",
                table: "api_keys");

            migrationBuilder.DropForeignKey(
                name: "FK_logs_Tenants_TenantId",
                table: "logs");

            migrationBuilder.DropForeignKey(
                name: "FK_services_Tenants_TenantId",
                table: "services");

            migrationBuilder.DropForeignKey(
                name: "FK_users_Tenants_TenantId",
                table: "users");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_users_TenantId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_services_TenantId",
                table: "services");

            migrationBuilder.DropIndex(
                name: "IX_logs_TenantId",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "idx_logs_clusterid",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "idx_incidents_service_start_status",
                table: "incidents");

            migrationBuilder.DropIndex(
                name: "IX_api_keys_TenantId",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "services");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "logs");

            migrationBuilder.DropColumn(
                name: "ErrorCount",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "FirstSeen",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "SuggestedCause",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "Template",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "WarningCount",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "RawApiKeyCiphertext",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "api_keys");

            migrationBuilder.RenameIndex(
                name: "idx_logs_incidentid",
                table: "logs",
                newName: "IX_logs_IncidentId");

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "logs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "api_keys",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}

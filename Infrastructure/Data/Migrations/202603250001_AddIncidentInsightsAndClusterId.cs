using System;
using LogLens.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogLens.Infrastructure.Data.Migrations
{
    [DbContext(typeof(LogLensDbContext))]
    [Migration("202603250001_AddIncidentInsightsAndClusterId")]
    public class AddIncidentInsightsAndClusterId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClusterId",
                table: "logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErrorCount",
                table: "incidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSeen",
                table: "incidents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "incidents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "incidents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "UnknownService");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "incidents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "SuggestedCause",
                table: "incidents",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "Template",
                table: "incidents",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "incidents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "Legacy Incident");

            migrationBuilder.AddColumn<int>(
                name: "WarningCount",
                table: "incidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE incidents
                SET
                    ""FirstSeen"" = COALESCE(""StartTime"", NOW()),
                    ""LastSeen"" = COALESCE(""EndTime"", ""StartTime"", NOW()),
                    ""Title"" = CASE
                        WHEN COALESCE(""Description"", '') = '' THEN 'Legacy Incident'
                        ELSE LEFT(""Description"", 256)
                    END,
                    ""Template"" = CASE
                        WHEN COALESCE(""Description"", '') = '' THEN 'legacy-template'
                        ELSE LEFT(""Description"", 2048)
                    END,
                    ""SuggestedCause"" = 'Migrated from legacy incident schema',
                    ""Status"" = CASE WHEN ""EndTime"" IS NULL THEN 'Active' ELSE 'Resolved' END,
                    ""ServiceName"" = 'UnknownService',
                    ""ErrorCount"" = 0,
                    ""WarningCount"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "idx_logs_clusterid",
                table: "logs",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "idx_logs_incidentid",
                table: "logs",
                column: "IncidentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "idx_logs_clusterid", table: "logs");
            migrationBuilder.DropIndex(name: "idx_logs_incidentid", table: "logs");

            migrationBuilder.DropColumn(name: "ClusterId", table: "logs");
            migrationBuilder.DropColumn(name: "ErrorCount", table: "incidents");
            migrationBuilder.DropColumn(name: "FirstSeen", table: "incidents");
            migrationBuilder.DropColumn(name: "LastSeen", table: "incidents");
            migrationBuilder.DropColumn(name: "ServiceName", table: "incidents");
            migrationBuilder.DropColumn(name: "Status", table: "incidents");
            migrationBuilder.DropColumn(name: "SuggestedCause", table: "incidents");
            migrationBuilder.DropColumn(name: "Template", table: "incidents");
            migrationBuilder.DropColumn(name: "Title", table: "incidents");
            migrationBuilder.DropColumn(name: "WarningCount", table: "incidents");
        }
    }
}

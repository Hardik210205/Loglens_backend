using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "forecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictedValue = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_forecasts_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_logs_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ForecastId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alerts_forecasts_ForecastId",
                        column: x => x.ForecastId,
                        principalTable: "forecasts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_alerts_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_alerts_forecastid",
                table: "alerts",
                column: "ForecastId");

            migrationBuilder.CreateIndex(
                name: "idx_alerts_incidentid",
                table: "alerts",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "idx_alerts_severity",
                table: "alerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "idx_alerts_timestamp",
                table: "alerts",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_alerts_timestamp_severity",
                table: "alerts",
                columns: new[] { "Timestamp", "Severity" });

            migrationBuilder.CreateIndex(
                name: "idx_forecasts_forecasttime",
                table: "forecasts",
                column: "ForecastTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_forecasts_incidentid",
                table: "forecasts",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "idx_forecasts_predictedvalue",
                table: "forecasts",
                column: "PredictedValue");

            migrationBuilder.CreateIndex(
                name: "idx_incidents_severity",
                table: "incidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "idx_incidents_starttime",
                table: "incidents",
                column: "StartTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_incidents_starttime_severity",
                table: "incidents",
                columns: new[] { "StartTime", "Severity" });

            migrationBuilder.CreateIndex(
                name: "idx_logs_level",
                table: "logs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "idx_logs_timestamp",
                table: "logs",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_logs_timestamp_level",
                table: "logs",
                columns: new[] { "Timestamp", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_logs_IncidentId",
                table: "logs",
                column: "IncidentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "logs");

            migrationBuilder.DropTable(
                name: "forecasts");

            migrationBuilder.DropTable(
                name: "incidents");
        }
    }
}

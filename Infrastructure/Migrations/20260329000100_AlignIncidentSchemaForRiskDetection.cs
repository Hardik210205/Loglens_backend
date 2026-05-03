using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogLens.Infrastructure.Migrations
{
    public partial class AlignIncidentSchemaForRiskDetection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'Title') THEN
        ALTER TABLE incidents ADD COLUMN ""Title"" character varying(256) NOT NULL DEFAULT '';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'Template') THEN
        ALTER TABLE incidents ADD COLUMN ""Template"" character varying(2048) NOT NULL DEFAULT '';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'ServiceName') THEN
        ALTER TABLE incidents ADD COLUMN ""ServiceName"" character varying(128) NOT NULL DEFAULT 'UnknownService';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'ErrorCount') THEN
        ALTER TABLE incidents ADD COLUMN ""ErrorCount"" integer NOT NULL DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'WarningCount') THEN
        ALTER TABLE incidents ADD COLUMN ""WarningCount"" integer NOT NULL DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'FirstSeen') THEN
        ALTER TABLE incidents ADD COLUMN ""FirstSeen"" timestamp with time zone NOT NULL DEFAULT NOW();
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'LastSeen') THEN
        ALTER TABLE incidents ADD COLUMN ""LastSeen"" timestamp with time zone NOT NULL DEFAULT NOW();
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'SuggestedCause') THEN
        ALTER TABLE incidents ADD COLUMN ""SuggestedCause"" character varying(512);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'Status') THEN
        ALTER TABLE incidents ADD COLUMN ""Status"" character varying(32) NOT NULL DEFAULT 'Active';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'StartTimeUtc') THEN
        ALTER TABLE incidents ADD COLUMN ""StartTimeUtc"" timestamp with time zone NOT NULL DEFAULT NOW();
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
DECLARE
    severity_type text;
BEGIN
    SELECT data_type INTO severity_type
    FROM information_schema.columns
    WHERE table_name = 'incidents' AND column_name = 'Severity';

    IF severity_type = 'integer' THEN
        ALTER TABLE incidents
            ALTER COLUMN ""Severity"" TYPE character varying(16)
            USING CASE ""Severity""
                WHEN 0 THEN 'low'
                WHEN 1 THEN 'medium'
                WHEN 2 THEN 'high'
                WHEN 3 THEN 'critical'
                ELSE 'low'
            END;
    ELSIF severity_type IS NULL THEN
        ALTER TABLE incidents ADD COLUMN ""Severity"" character varying(16) NOT NULL DEFAULT 'low';
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
DECLARE
    start_expr text := 'NOW()';
    end_expr text := 'NOW()';
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'StartTime') THEN
        start_expr := '""StartTime""';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'incidents' AND column_name = 'EndTime') THEN
        end_expr := '""EndTime""';
    ELSIF start_expr <> 'NOW()' THEN
        end_expr := start_expr;
    END IF;

    EXECUTE format(
        'UPDATE incidents
         SET
            ""ServiceName"" = COALESCE(NULLIF(""ServiceName"", ''''), ''UnknownService''),
            ""Title"" = COALESCE(NULLIF(""Title"", ''''), ''Detected incident''),
            ""Template"" = COALESCE(NULLIF(""Template"", ''''), ''legacy-incident''),
            ""FirstSeen"" = COALESCE(""FirstSeen"", %1$s, NOW()),
            ""LastSeen"" = COALESCE(""LastSeen"", %2$s, %1$s, NOW()),
            ""StartTimeUtc"" = COALESCE(""StartTimeUtc"", ""FirstSeen"", %1$s, NOW()),
            ""Status"" = COALESCE(NULLIF(""Status"", ''''), ''Active''),
            ""Severity"" = COALESCE(NULLIF(""Severity"", ''''), ''low'')',
        start_expr,
        end_expr);
END $$;
");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_starttime;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_starttime_severity;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_severity;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_service_start_status;");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_incidents_starttime
ON incidents (""StartTimeUtc"" DESC);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_incidents_starttime_severity
ON incidents (""StartTimeUtc"", ""Severity"");
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_incidents_severity
ON incidents (""Severity"");
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_incidents_service_start_status
ON incidents (""ServiceName"", ""StartTimeUtc"", ""Status"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_service_start_status;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_starttime_severity;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_starttime;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_incidents_severity;");
        }
    }
}

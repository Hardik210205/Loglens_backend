using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogLens.Infrastructure.Migrations
{
    public partial class AddRecoverableApiKeyCipher : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawApiKeyCiphertext",
                table: "api_keys",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawApiKeyCiphertext",
                table: "api_keys");
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RenewalWebsite.Data.Migrations
{
    public partial class donations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EventID",
                table: "EventLog",
                newName: "EventId");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "EventLog",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EventLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogLevel",
                table: "EventLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "Donations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Donations",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Donations");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "EventLog",
                newName: "EventID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "EventLog",
                newName: "ID");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EventLog",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogLevel",
                table: "EventLog",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}

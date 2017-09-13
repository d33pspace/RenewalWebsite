using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RenewalWebsite.Data.Migrations
{
    public partial class datatypechange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
               name: "DonationAmount",
               table: "Donations",
               type: "decimal(18, 2)",
               nullable: true,
               oldClrType: typeof(int),
               oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
               name: "DonationAmount",
               table: "Donations",
               nullable: true,
               oldClrType: typeof(decimal),
               oldType: "decimal(18, 2)",
               oldNullable: true);
        }
    }
}

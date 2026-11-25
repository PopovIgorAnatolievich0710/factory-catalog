using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tenengroup.Balancer.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentAndSupplierContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "working_days",
                schema: "balancer",
                table: "shipping_route_cache",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "reproduction_requested_at",
                schema: "balancer",
                table: "allocation",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "working_days",
                schema: "balancer",
                table: "shipping_route_cache");

            migrationBuilder.DropColumn(
                name: "reproduction_requested_at",
                schema: "balancer",
                table: "allocation");
        }
    }
}

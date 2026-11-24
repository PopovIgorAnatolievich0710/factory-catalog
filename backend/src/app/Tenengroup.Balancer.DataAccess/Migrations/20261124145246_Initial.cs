using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tenengroup.Balancer.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "balancer");

            migrationBuilder.CreateTable(
                name: "allocation",
                schema: "balancer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sku_uuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    fulfillment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    destination_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    promised_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    sla_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_event_order = table.Column<bool>(type: "boolean", nullable: false),
                    event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    site = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cbd = table.Column<string>(type: "jsonb", nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fallback_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    quote_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    selected_quote_id = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    original_factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    packing_facility_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    production_page = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    material_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    labor_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    packing_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    production_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    shipping_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    cheapest_feasible_total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_labor_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    eta_from = table.Column<DateOnly>(type: "date", nullable: true),
                    eta_to = table.Column<DateOnly>(type: "date", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    meets_promise = table.Column<bool>(type: "boolean", nullable: true),
                    is_split = table.Column<bool>(type: "boolean", nullable: false),
                    decision_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    candidates = table.Column<string>(type: "jsonb", nullable: false),
                    settings_version = table.Column<int>(type: "integer", nullable: false),
                    transfer_count = table.Column<int>(type: "integer", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    output_published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    produced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allocation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "balancer_settings",
                schema: "balancer",
                columns: table => new
                {
                    version = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    late_penalty_per_day = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    split_penalty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    share_weight = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    success_rate_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    success_rate_weight = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    share_window_days = table.Column<int>(type: "integer", nullable: false),
                    share_gap_threshold_pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    default_production_days = table.Column<int>(type: "integer", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_balancer_settings", x => x.version);
                });

            migrationBuilder.CreateTable(
                name: "capacity_reservation",
                schema: "balancer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    reserved_pct = table.Column<int>(type: "integer", nullable: false),
                    event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_capacity_reservation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "legacy_decision",
                schema: "balancer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    eta = table.Column<DateOnly>(type: "date", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_legacy_decision", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_route_cache",
                schema: "balancer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    origin_facility_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    destination_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    cost_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    transit_days_min = table.Column<int>(type: "integer", nullable: false),
                    transit_days_max = table.Column<int>(type: "integer", nullable: false),
                    refreshed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_route_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "allocation_cost_line",
                schema: "balancer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quoted_unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    quoted_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allocation_cost_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_allocation_cost_line_allocation_allocation_id",
                        column: x => x.allocation_id,
                        principalSchema: "balancer",
                        principalTable: "allocation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "allocation_transfer",
                schema: "balancer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sku_uuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    from_factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    from_page = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    to_factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    to_page = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    old_quote_id = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    new_quote_id = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    cost_delta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    old_eta = table.Column<DateOnly>(type: "date", nullable: true),
                    new_eta = table.Column<DateOnly>(type: "date", nullable: true),
                    transferred_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    initiator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transferred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allocation_transfer", x => x.id);
                    table.ForeignKey(
                        name: "fk_allocation_transfer_allocation_allocation_id",
                        column: x => x.allocation_id,
                        principalSchema: "balancer",
                        principalTable: "allocation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production_record",
                schema: "balancer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sku_uuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quote_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quote_id = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    packing_facility_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    production_days = table.Column<int>(type: "integer", nullable: false),
                    is_reproduction = table.Column<bool>(type: "boolean", nullable: false),
                    reproduction_of_quote_id = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    reproduction_of_record_id = table.Column<long>(type: "bigint", nullable: true),
                    production_log = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_record_allocation_allocation_id",
                        column: x => x.allocation_id,
                        principalSchema: "balancer",
                        principalTable: "allocation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_record_production_record_reproduction_of_record_",
                        column: x => x.reproduction_of_record_id,
                        principalSchema: "balancer",
                        principalTable: "production_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_decided_at",
                schema: "balancer",
                table: "allocation",
                column: "decided_at");

            migrationBuilder.CreateIndex(
                name: "ix_allocation_factory_id_status_due_date",
                schema: "balancer",
                table: "allocation",
                columns: new[] { "factory_id", "status", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_order_id_order_item_id",
                schema: "balancer",
                table: "allocation",
                columns: new[] { "order_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_cost_line_allocation_id",
                schema: "balancer",
                table: "allocation_cost_line",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_allocation_transfer_allocation_id",
                schema: "balancer",
                table: "allocation_transfer",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_allocation_transfer_order_id_order_item_id",
                schema: "balancer",
                table: "allocation_transfer",
                columns: new[] { "order_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_transfer_transferred_at",
                schema: "balancer",
                table: "allocation_transfer",
                column: "transferred_at");

            migrationBuilder.CreateIndex(
                name: "ix_capacity_reservation_factory_id_date_from_date_to",
                schema: "balancer",
                table: "capacity_reservation",
                columns: new[] { "factory_id", "date_from", "date_to" });

            migrationBuilder.CreateIndex(
                name: "ix_legacy_decision_order_id_order_item_id",
                schema: "balancer",
                table: "legacy_decision",
                columns: new[] { "order_id", "order_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_production_record_allocation_id",
                schema: "balancer",
                table: "production_record",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_record_factory_id_sent_at",
                schema: "balancer",
                table: "production_record",
                columns: new[] { "factory_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_production_record_reproduction_of_record_id",
                schema: "balancer",
                table: "production_record",
                column: "reproduction_of_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_record_source_event_id",
                schema: "balancer",
                table: "production_record",
                column: "source_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipping_route_cache_destination_country",
                schema: "balancer",
                table: "shipping_route_cache",
                column: "destination_country");

            migrationBuilder.CreateIndex(
                name: "ix_shipping_route_cache_origin_facility_id_destination_country",
                schema: "balancer",
                table: "shipping_route_cache",
                columns: new[] { "origin_facility_id", "destination_country" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allocation_cost_line",
                schema: "balancer");

            migrationBuilder.DropTable(
                name: "allocation_transfer",
                schema: "balancer");

            migrationBuilder.DropTable(
                name: "balancer_settings",
                schema: "balancer");

            migrationBuilder.DropTable(
                name: "capacity_reservation",
                schema: "balancer");

            migrationBuilder.DropTable(
                name: "legacy_decision",
                schema: "balancer");

            migrationBuilder.DropTable(
                name: "production_record",
                schema: "balancer");

            migrationBuilder.DropTable(
                name: "shipping_route_cache",
                schema: "balancer");

            migrationBuilder.DropTable(
                name: "allocation",
                schema: "balancer");
        }
    }
}

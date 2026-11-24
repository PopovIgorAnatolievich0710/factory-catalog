using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tenengroup.FactoryCatalog.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "balancer_skip_log",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sku_uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    component_erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    process_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_balancer_skip_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "component",
                schema: "catalog",
                columns: table => new
                {
                    erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    family = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    material = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    measured_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    measure_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    measure_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unit_of_measure_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_of_measure_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    unit_of_measure_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country_of_origin = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    hs_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unit_cost_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    unit_cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    unit_cost_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    critical_stock_level = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    last_sap_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_component", x => x.erp_id);
                });

            migrationBuilder.CreateTable(
                name: "component_replacement",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    original_erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    replacement_erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    replaced_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    replaced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_component_replacement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_factory", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_toggle",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_toggle", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "history_entry",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_history_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "process",
                schema: "catalog",
                columns: table => new
                {
                    process_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    measured_in = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process", x => x.process_id);
                });

            migrationBuilder.CreateTable(
                name: "quote_set",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quote_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sku_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cbd = table.Column<string>(type: "jsonb", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote_set", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sku",
                schema: "catalog",
                columns: table => new
                {
                    sku_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    site_sku = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    product_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dropshipper_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dropshipper_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dropshipper_total_cost_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    dropshipper_total_cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    validation_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    validation_issues = table.Column<List<string>>(type: "text[]", nullable: false),
                    last_master_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku", x => x.sku_uuid);
                });

            migrationBuilder.CreateTable(
                name: "sync_run",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sync_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    added = table.Column<int>(type: "integer", nullable: false),
                    updated = table.Column<int>(type: "integer", nullable: false),
                    failed = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    triggered_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_run", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_list_settings",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    list_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    columns = table.Column<List<string>>(type: "text[]", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_list_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "component_alternative",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    component_erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    alternative_erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    for_balancer = table.Column<bool>(type: "boolean", nullable: false),
                    for_planner = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_component_alternative", x => x.id);
                    table.ForeignKey(
                        name: "fk_component_alternative_component_alternative_erp_id",
                        column: x => x.alternative_erp_id,
                        principalSchema: "catalog",
                        principalTable: "component",
                        principalColumn: "erp_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_component_alternative_component_component_erp_id",
                        column: x => x.component_erp_id,
                        principalSchema: "catalog",
                        principalTable: "component",
                        principalColumn: "erp_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "component_factory",
                schema: "catalog",
                columns: table => new
                {
                    component_erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    warehouse_stock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    production_floor_stock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    stock_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    critical_stock_level_override = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    manual_oos_active = table.Column<bool>(type: "boolean", nullable: false),
                    manual_oos_set_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    manual_oos_set_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    manual_oos_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_component_factory", x => new { x.component_erp_id, x.factory_id });
                    table.ForeignKey(
                        name: "fk_component_factory_component_component_erp_id",
                        column: x => x.component_erp_id,
                        principalSchema: "catalog",
                        principalTable: "component",
                        principalColumn: "erp_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_component_factory_factories_factory_id",
                        column: x => x.factory_id,
                        principalSchema: "catalog",
                        principalTable: "factory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "global_factory_share",
                schema: "catalog",
                columns: table => new
                {
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_global_factory_share", x => x.factory_id);
                    table.ForeignKey(
                        name: "fk_global_factory_share_factory_factory_id",
                        column: x => x.factory_id,
                        principalSchema: "catalog",
                        principalTable: "factory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_factory",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_factory", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_factory_factory_factory_id",
                        column: x => x.factory_id,
                        principalSchema: "catalog",
                        principalTable: "factory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_factory",
                schema: "catalog",
                columns: table => new
                {
                    process_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    runs_here = table.Column<bool>(type: "boolean", nullable: false),
                    machine_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    capacity_pct = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    capacity_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    expected_recovery = table.Column<DateOnly>(type: "date", nullable: true),
                    capacity_updated_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    capacity_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_per_day = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    labor_cost_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    labor_cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    labor_cost_effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_factory", x => new { x.process_id, x.factory_id });
                    table.CheckConstraint("ck_process_factory_capacity_pct", "capacity_pct BETWEEN 0 AND 100 AND capacity_pct % 10 = 0");
                    table.ForeignKey(
                        name: "fk_process_factory_factories_factory_id",
                        column: x => x.factory_id,
                        principalSchema: "catalog",
                        principalTable: "factory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_process_factory_process_process_id",
                        column: x => x.process_id,
                        principalSchema: "catalog",
                        principalTable: "process",
                        principalColumn: "process_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quote",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quote_id = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    quote_set_id = table.Column<long>(type: "bigint", nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    producible = table.Column<bool>(type: "boolean", nullable: false),
                    reasons = table.Column<List<string>>(type: "text[]", nullable: false),
                    material_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    labor_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    packing_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    capacity_signal_pct = table.Column<int>(type: "integer", nullable: true),
                    share_pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    components = table.Column<string>(type: "jsonb", nullable: false),
                    processes = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    selected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    produced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    transferred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reproduction_of_quote_id = table.Column<long>(type: "bigint", nullable: true),
                    production_log = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote", x => x.id);
                    table.ForeignKey(
                        name: "fk_quote_factories_factory_id",
                        column: x => x.factory_id,
                        principalSchema: "catalog",
                        principalTable: "factory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quote_quote_reproduction_of_quote_id",
                        column: x => x.reproduction_of_quote_id,
                        principalSchema: "catalog",
                        principalTable: "quote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quote_quote_sets_quote_set_id",
                        column: x => x.quote_set_id,
                        principalSchema: "catalog",
                        principalTable: "quote_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sku_alias",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    deprecated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_alias", x => x.id);
                    table.ForeignKey(
                        name: "fk_sku_alias_sku_sku_uuid",
                        column: x => x.sku_uuid,
                        principalSchema: "catalog",
                        principalTable: "sku",
                        principalColumn: "sku_uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sku_component",
                schema: "catalog",
                columns: table => new
                {
                    sku_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    component_erp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    usage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_component", x => new { x.sku_uuid, x.component_erp_id });
                    table.ForeignKey(
                        name: "fk_sku_component_sku_sku_uuid",
                        column: x => x.sku_uuid,
                        principalSchema: "catalog",
                        principalTable: "sku",
                        principalColumn: "sku_uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sku_factory",
                schema: "catalog",
                columns: table => new
                {
                    sku_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    factory_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    producible = table.Column<bool>(type: "boolean", nullable: false),
                    override_share_pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    override_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    override_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_factory", x => new { x.sku_uuid, x.factory_id });
                    table.ForeignKey(
                        name: "fk_sku_factory_factory_factory_id",
                        column: x => x.factory_id,
                        principalSchema: "catalog",
                        principalTable: "factory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sku_factory_sku_sku_uuid",
                        column: x => x.sku_uuid,
                        principalSchema: "catalog",
                        principalTable: "sku",
                        principalColumn: "sku_uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sku_process",
                schema: "catalog",
                columns: table => new
                {
                    sku_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    process_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_process", x => new { x.sku_uuid, x.process_id });
                    table.ForeignKey(
                        name: "fk_sku_process_sku_sku_uuid",
                        column: x => x.sku_uuid,
                        principalSchema: "catalog",
                        principalTable: "sku",
                        principalColumn: "sku_uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "factory",
                columns: new[] { "id", "country_code", "is_active", "name" },
                values: new object[,]
                {
                    { "110", null, true, "Factory 110" },
                    { "125", null, true, "Factory 125" },
                    { "315", null, true, "Factory 315" }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "process",
                columns: new[] { "process_id", "created_at", "description", "measured_in", "name", "status", "type", "updated_at" },
                values: new object[] { "PACKING", new DateTime(2026, 11, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Packing of a finished order item", "Units", "Packing", "Active", "Work", new DateTime(2026, 11, 24, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "ix_balancer_skip_log_created_at",
                schema: "catalog",
                table: "balancer_skip_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_balancer_skip_log_order_id",
                schema: "catalog",
                table: "balancer_skip_log",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_component_family",
                schema: "catalog",
                table: "component",
                column: "family");

            migrationBuilder.CreateIndex(
                name: "ix_component_name",
                schema: "catalog",
                table: "component",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_component_status",
                schema: "catalog",
                table: "component",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_component_alternative_alternative_erp_id",
                schema: "catalog",
                table: "component_alternative",
                column: "alternative_erp_id");

            migrationBuilder.CreateIndex(
                name: "ix_component_alternative_component_erp_id_alternative_erp_id",
                schema: "catalog",
                table: "component_alternative",
                columns: new[] { "component_erp_id", "alternative_erp_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_component_factory_factory_id_manual_oos_active",
                schema: "catalog",
                table: "component_factory",
                columns: new[] { "factory_id", "manual_oos_active" });

            migrationBuilder.CreateIndex(
                name: "ix_component_replacement_order_id_order_item_id",
                schema: "catalog",
                table: "component_replacement",
                columns: new[] { "order_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_component_replacement_original_erp_id",
                schema: "catalog",
                table: "component_replacement",
                column: "original_erp_id");

            migrationBuilder.CreateIndex(
                name: "ix_feature_toggle_name",
                schema: "catalog",
                table: "feature_toggle",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_history_entry_entity_type_entity_id_changed_at",
                schema: "catalog",
                table: "history_entry",
                columns: new[] { "entity_type", "entity_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_process_name",
                schema: "catalog",
                table: "process",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_process_factory_factory_id",
                schema: "catalog",
                table: "process_factory",
                column: "factory_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_factory_id_status_produced_at",
                schema: "catalog",
                table: "quote",
                columns: new[] { "factory_id", "status", "produced_at" });

            migrationBuilder.CreateIndex(
                name: "ix_quote_quote_id",
                schema: "catalog",
                table: "quote",
                column: "quote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quote_quote_set_id",
                schema: "catalog",
                table: "quote",
                column: "quote_set_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_reproduction_of_quote_id",
                schema: "catalog",
                table: "quote",
                column: "reproduction_of_quote_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_set_order_id_order_item_id",
                schema: "catalog",
                table: "quote_set",
                columns: new[] { "order_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_quote_set_quote_key",
                schema: "catalog",
                table: "quote_set",
                column: "quote_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quote_set_request_id",
                schema: "catalog",
                table: "quote_set",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sku_name",
                schema: "catalog",
                table: "sku",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_sku_product_type_status",
                schema: "catalog",
                table: "sku",
                columns: new[] { "product_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sku_site_sku",
                schema: "catalog",
                table: "sku",
                column: "site_sku");

            migrationBuilder.CreateIndex(
                name: "ix_sku_validation_status",
                schema: "catalog",
                table: "sku",
                column: "validation_status");

            migrationBuilder.CreateIndex(
                name: "ix_sku_alias_alias",
                schema: "catalog",
                table: "sku_alias",
                column: "alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sku_alias_sku_uuid",
                schema: "catalog",
                table: "sku_alias",
                column: "sku_uuid");

            migrationBuilder.CreateIndex(
                name: "ix_sku_component_component_erp_id",
                schema: "catalog",
                table: "sku_component",
                column: "component_erp_id");

            migrationBuilder.CreateIndex(
                name: "ix_sku_factory_factory_id_producible",
                schema: "catalog",
                table: "sku_factory",
                columns: new[] { "factory_id", "producible" });

            migrationBuilder.CreateIndex(
                name: "ix_sku_process_process_id",
                schema: "catalog",
                table: "sku_process",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_run_sync_type_started_at",
                schema: "catalog",
                table: "sync_run",
                columns: new[] { "sync_type", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ux_sync_run_sync_type_running",
                schema: "catalog",
                table: "sync_run",
                column: "sync_type",
                unique: true,
                filter: "status = 'Running'");

            migrationBuilder.CreateIndex(
                name: "ix_user_factory_factory_id",
                schema: "catalog",
                table: "user_factory",
                column: "factory_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_factory_user_email_factory_id",
                schema: "catalog",
                table: "user_factory",
                columns: new[] { "user_email", "factory_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_list_settings_user_email_list_key",
                schema: "catalog",
                table: "user_list_settings",
                columns: new[] { "user_email", "list_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balancer_skip_log",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "component_alternative",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "component_factory",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "component_replacement",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "feature_toggle",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "global_factory_share",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "history_entry",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "process_factory",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "quote",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "sku_alias",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "sku_component",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "sku_factory",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "sku_process",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "sync_run",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "user_factory",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "user_list_settings",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "component",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "process",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "quote_set",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "sku",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "factory",
                schema: "catalog");
        }
    }
}

BEGIN;

SET LOCAL TIME ZONE 'UTC';

CREATE TEMP TABLE demo_component_ids(erp_id text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO demo_component_ids VALUES
('R.WG.10K.BRD.08MM'),('R.WG.10K.BRD.09MM'),('R.VM.BRD.08MM'),('R.SS.BRD.08MM'),('R.YG.14K.BRD.08MM'),('R.RG.14K.BRD.08MM'),
('CHN.VM.CABLE'),('CHN.VM.CABLE.45'),('CHN.SS.CABLE.45'),('CHN.SS.ROLO.50'),('CHN.WG.10K.BOX.45'),('CHN.YG.14K.FIG.50'),
('CLS.LOB.10MM.VM'),('CLS.LOB.10MM.SS'),('CLS.SPR.6MM.SS'),('STN.DIA.RND.002'),('STN.DIA.RND.005'),('STN.CZ.RND.3MM'),
('STN.BRT.GAR.4MM'),('STN.BRT.SAP.4MM'),('STN.BRT.EME.4MM'),('JR.SS.5MM'),('JR.VM.5MM'),('PKG.BOX.LOGO'),('PKG.POUCH.VEL'),
('PKG.CARD.CARE'),('PLT.SOL.24K'),('PLT.SOL.RH'),('WIR.SS.08MM'),('CHM.HRT.SS'),('STR.LTH.BRN.20'),('PKG.BOX.XMAS');

CREATE TEMP TABLE demo_process_ids(process_id text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO demo_process_ids VALUES
('CUTTING'),('ENGRAVING'),('CNC_MILLING'),('POLISHING'),('PLATING'),('RHODIUM'),('SETTING'),('SOLDERING'),('ASSEMBLY'),('QC');

DELETE FROM balancer.production_record
WHERE reproduction_of_record_id IS NOT NULL AND (sku_uuid LIKE 'de300000-%' OR order_id LIKE 'ORD8843%');
DELETE FROM balancer.allocation WHERE sku_uuid LIKE 'de300000-%' OR order_id LIKE 'ORD8843%';
DELETE FROM balancer.legacy_decision WHERE order_id LIKE 'ORD8843%';
DELETE FROM balancer.capacity_reservation WHERE updated_by = 'ravit.tal@tenengroup.com';

DELETE FROM catalog.quote
WHERE reproduction_of_quote_id IS NOT NULL
  AND quote_set_id IN (SELECT id FROM catalog.quote_set WHERE sku_uuid::text LIKE 'de300000-%');
DELETE FROM catalog.quote_set WHERE sku_uuid::text LIKE 'de300000-%';
DELETE FROM catalog.sku WHERE sku_uuid::text LIKE 'de300000-%';
DELETE FROM catalog.component_alternative
WHERE component_erp_id IN (SELECT erp_id FROM demo_component_ids)
   OR alternative_erp_id IN (SELECT erp_id FROM demo_component_ids);
DELETE FROM catalog.component WHERE erp_id IN (SELECT erp_id FROM demo_component_ids);
DELETE FROM catalog.process_factory
WHERE process_id IN (SELECT process_id FROM demo_process_ids) OR process_id = 'PACKING';
DELETE FROM catalog.process WHERE process_id IN (SELECT process_id FROM demo_process_ids);
DELETE FROM catalog.history_entry
WHERE entity_id IN (SELECT erp_id FROM demo_component_ids)
   OR entity_id IN (SELECT process_id FROM demo_process_ids)
   OR entity_id LIKE 'de300000-%'
   OR (entity_type = 'global-factory-share' AND changed_by = 'dana.ofer@tenengroup.com');
DELETE FROM catalog.sync_run WHERE triggered_by = 'dana.ofer@tenengroup.com';

UPDATE catalog.factory SET country_code = 'IL' WHERE id IN ('110', '125') AND country_code IS NULL;
UPDATE catalog.factory SET country_code = 'HU' WHERE id = '315' AND country_code IS NULL;

INSERT INTO catalog.sync_run(sync_type, status, started_at, finished_at, added, updated, failed, error, triggered_by) VALUES
('sap-components', 'Succeeded', now() - interval '3 hours 2 minutes', now() - interval '3 hours', 32, 0, 0, NULL, 'dana.ofer@tenengroup.com'),
('master-catalog-skus', 'Succeeded', now() - interval '2 hours 1 minute', now() - interval '2 hours', 24, 0, 0, NULL, 'dana.ofer@tenengroup.com');

INSERT INTO catalog.component(erp_id, name, status, family, material, measured_by, measure_value, measure_unit,
    unit_of_measure_type, unit_of_measure_quantity, unit_of_measure_unit, country_of_origin, hs_code,
    unit_cost_amount, unit_cost_currency, unit_cost_source, critical_stock_level, last_sap_sync_at, created_at, updated_at)
SELECT v.erp_id, v.name, v.status, v.family, v.material, v.measured_by, v.measure_value, v.measure_unit,
       v.uom_type, v.uom_qty, v.uom_unit, v.origin, v.hs, v.cost, 'USD', 'SAP', v.critical,
       now() - interval '3 hours', now() - interval '120 days', now() - interval '3 hours'
FROM (VALUES
('R.WG.10K.BRD.08MM', 'White gold board 10K 0.8', 'Active', 'Board', '10K white gold', 'thickness', 0.8, 'mm', 'weight', 1, 'kg', 'US', '7113.11.00', 245.5, 2),
('R.WG.10K.BRD.09MM', 'White gold board 10K 0.9', 'Active', 'Board', '10K white gold', 'thickness', 0.9, 'mm', 'weight', 1, 'kg', 'US', '7113.11.00', 268.0, 2),
('R.VM.BRD.08MM', 'Vermeil board 0.8', 'Active', 'Board', 'Gold vermeil', 'thickness', 0.8, 'mm', 'weight', 1, 'kg', 'IT', '7113.11.00', 96.4, 3),
('R.SS.BRD.08MM', 'Sterling silver board 0.8', 'Active', 'Board', '925 sterling silver', 'thickness', 0.8, 'mm', 'weight', 1, 'kg', 'IT', '7106.92.00', 58.2, 5),
('R.YG.14K.BRD.08MM', 'Yellow gold board 14K 0.8', 'Active', 'Board', '14K yellow gold', 'thickness', 0.8, 'mm', 'weight', 1, 'kg', 'US', '7108.13.00', 312.75, 1.5),
('R.RG.14K.BRD.08MM', 'Rose gold board 14K 0.8', 'Active', 'Board', '14K rose gold', 'thickness', 0.8, 'mm', 'weight', 1, 'kg', 'US', '7108.13.00', 309.9, 1.5),
('CHN.VM.CABLE', 'Vermeil cable chain', 'Active', 'Chain', 'Gold vermeil', 'length', 40, 'cm', 'count', 1, 'pcs', 'IT', '7113.11.00', 4.85, 400),
('CHN.VM.CABLE.45', 'Vermeil cable chain 45 cm', 'Active', 'Chain', 'Gold vermeil', 'length', 45, 'cm', 'count', 1, 'pcs', 'IT', '7113.11.00', 5.10, 300),
('CHN.SS.CABLE.45', 'Sterling silver cable chain 45 cm', 'Active', 'Chain', '925 sterling silver', 'length', 45, 'cm', 'count', 1, 'pcs', 'IT', '7113.11.00', 2.95, 500),
('CHN.SS.ROLO.50', 'Sterling silver rolo chain 50 cm', 'Active', 'Chain', '925 sterling silver', 'length', 50, 'cm', 'count', 1, 'pcs', 'IT', '7113.11.00', 3.40, 250),
('CHN.WG.10K.BOX.45', 'White gold box chain 10K 45 cm', 'Active', 'Chain', '10K white gold', 'length', 45, 'cm', 'count', 1, 'pcs', 'US', '7113.19.00', 38.5, 60),
('CHN.YG.14K.FIG.50', 'Yellow gold figaro chain 14K 50 cm', 'Active', 'Chain', '14K yellow gold', 'length', 50, 'cm', 'count', 1, 'pcs', 'US', '7113.19.00', 64.2, 40),
('CLS.LOB.10MM.VM', 'Lobster clasp 10 mm vermeil', 'Active', 'Clasp', 'Gold vermeil', 'length', 10, 'mm', 'count', 1, 'pcs', 'CN', '7113.11.00', 0.42, 1000),
('CLS.LOB.10MM.SS', 'Lobster clasp 10 mm silver', 'Active', 'Clasp', '925 sterling silver', 'length', 10, 'mm', 'count', 1, 'pcs', 'CN', '7113.11.00', 0.28, 1200),
('CLS.SPR.6MM.SS', 'Spring ring clasp 6 mm silver', 'Active', 'Clasp', '925 sterling silver', 'diameter', 6, 'mm', 'count', 1, 'pcs', 'CN', '7113.11.00', 0.18, 800),
('STN.DIA.RND.002', 'Diamond round 0.02 ct', 'Active', 'Stone', 'Natural diamond', 'weight', 0.02, 'ct', 'count', 1, 'pcs', 'IN', '7102.39.00', 14.6, 150),
('STN.DIA.RND.005', 'Diamond round 0.05 ct', 'Active', 'Stone', 'Natural diamond', 'weight', 0.05, 'ct', 'count', 1, 'pcs', 'IN', '7102.39.00', 33.0, 80),
('STN.CZ.RND.3MM', 'Cubic zirconia round 3 mm', 'Active', 'Stone', 'Cubic zirconia', 'diameter', 3, 'mm', 'count', 1, 'pcs', 'CN', '7104.99.00', 0.06, 3000),
('STN.BRT.GAR.4MM', 'Birthstone garnet 4 mm', 'Active', 'Stone', 'Garnet', 'diameter', 4, 'mm', 'count', 1, 'pcs', 'TH', '7103.99.00', 0.85, 200),
('STN.BRT.SAP.4MM', 'Birthstone sapphire (lab) 4 mm', 'Active', 'Stone', 'Lab-grown sapphire', 'diameter', 4, 'mm', 'count', 1, 'pcs', 'TH', '7104.91.00', 1.10, 200),
('STN.BRT.EME.4MM', 'Birthstone emerald (lab) 4 mm', 'Active', 'Stone', 'Lab-grown emerald', 'diameter', 4, 'mm', 'count', 1, 'pcs', 'TH', '7104.91.00', 1.25, 200),
('JR.SS.5MM', 'Jump ring 5 mm silver', 'Active', 'Jump ring', '925 sterling silver', 'diameter', 5, 'mm', 'count', 1, 'pcs', 'CN', '7113.11.00', 0.03, 5000),
('JR.VM.5MM', 'Jump ring 5 mm vermeil', 'Active', 'Jump ring', 'Gold vermeil', 'diameter', 5, 'mm', 'count', 1, 'pcs', 'CN', '7113.11.00', 0.05, 5000),
('PKG.BOX.LOGO', 'Gift box with logo', 'Active', 'Packaging', 'Cardboard', 'size', 8, 'cm', 'count', 1, 'pcs', 'IL', '4819.20.00', 0.65, 2000),
('PKG.POUCH.VEL', 'Velvet pouch', 'Active', 'Packaging', 'Velvet', 'size', 10, 'cm', 'count', 1, 'pcs', 'CN', '4202.32.00', 0.32, 1500),
('PKG.CARD.CARE', 'Jewelry care card', 'Active', 'Packaging', 'Paper', 'size', 9, 'cm', 'count', 1, 'pcs', 'IL', '4911.99.00', 0.04, 3000),
('PLT.SOL.24K', 'Gold plating solution 24K', 'Active', 'Plating', '24K gold solution', 'volume', 1, 'l', 'volume', 1, 'l', 'DE', '3824.99.00', 410.0, 4),
('PLT.SOL.RH', 'Rhodium plating solution', 'Active', 'Plating', 'Rhodium solution', 'volume', 1, 'l', 'volume', 1, 'l', 'DE', '3824.99.00', 980.0, 2),
('WIR.SS.08MM', 'Sterling silver wire 0.8 mm', 'Active', 'Wire', '925 sterling silver', 'thickness', 0.8, 'mm', 'weight', 1, 'kg', 'IT', '7106.92.00', 61.3, 3),
('CHM.HRT.SS', 'Heart charm silver', 'Inactive', 'Charm', '925 sterling silver', 'size', 8, 'mm', 'count', 1, 'pcs', 'CN', '7113.11.00', 0.74, 300),
('STR.LTH.BRN.20', 'Leather watch strap 20 mm brown', 'Inactive', 'Strap', 'Leather', 'width', 20, 'mm', 'count', 1, 'pcs', 'IT', '4203.30.00', 6.90, NULL),
('PKG.BOX.XMAS', 'Christmas gift box (seasonal)', 'Inactive', 'Packaging', 'Cardboard', 'size', 8, 'cm', 'count', 1, 'pcs', 'IL', '4819.20.00', 0.95, NULL)
) AS v(erp_id, name, status, family, material, measured_by, measure_value, measure_unit, uom_type, uom_qty, uom_unit, origin, hs, cost, critical);

INSERT INTO catalog.component_factory(component_erp_id, factory_id, warehouse_stock, production_floor_stock, stock_synced_at,
    critical_stock_level_override, manual_oos_active, manual_oos_set_by, manual_oos_set_at, manual_oos_note)
SELECT s.erp_id, f.factory_id, f.wh, f.fl, now() - interval '3 hours', NULL, false, NULL, NULL, NULL
FROM (VALUES
('R.WG.10K.BRD.08MM', 12.4, 1.1, 6.0, 0.5, 4.2, 0.8),
('R.WG.10K.BRD.09MM', 8.5, 0.6, 5.2, 0.4, 3.1, 0.5),
('R.VM.BRD.08MM', 18.2, 2.4, 14.6, 1.9, 9.8, 1.2),
('R.SS.BRD.08MM', 42.0, 4.5, 35.5, 3.8, 22.4, 2.6),
('R.YG.14K.BRD.08MM', 4.6, 0.4, 3.2, 0.3, 2.1, 0.2),
('R.RG.14K.BRD.08MM', 3.8, 0.3, 2.6, 0.2, 1.9, 0.2),
('CHN.VM.CABLE', 2400, 310, 1850, 240, 960, 120),
('CHN.VM.CABLE.45', 1900, 220, 1400, 160, 360, 50),
('CHN.SS.CABLE.45', 3200, 400, 2600, 310, 1500, 180),
('CHN.SS.ROLO.50', 1100, 140, 900, 95, 640, 70),
('CHN.WG.10K.BOX.45', 30, 5, 140, 12, 95, 10),
('CHN.YG.14K.FIG.50', 120, 14, 85, 9, 60, 6),
('CLS.LOB.10MM.VM', 5200, 600, 4100, 450, 520, 80),
('CLS.LOB.10MM.SS', 6100, 700, 4800, 520, 3300, 400),
('CLS.SPR.6MM.SS', 3900, 420, 3100, 300, 2600, 280),
('STN.DIA.RND.002', 820, 60, 35, 5, 410, 30),
('STN.DIA.RND.005', 260, 20, 50, 6, 150, 12),
('STN.CZ.RND.3MM', 18000, 2200, 12500, 1500, 8400, 900),
('STN.BRT.GAR.4MM', 940, 80, 760, 60, 480, 40),
('STN.BRT.SAP.4MM', 880, 70, 690, 55, 410, 35),
('STN.BRT.EME.4MM', 720, 60, 610, 50, 0, 0),
('JR.SS.5MM', 42000, 5100, 36000, 4200, 24000, 2600),
('JR.VM.5MM', 38000, 4600, 30500, 3900, 19800, 2100),
('PKG.BOX.LOGO', 14500, 900, 11800, 700, 1200, 150),
('PKG.POUCH.VEL', 9800, 600, 1100, 90, 5200, 300),
('PKG.CARD.CARE', 26000, 1500, 21000, 1200, 15000, 800),
('PLT.SOL.24K', 18, 3, 14, 2, 9, 1.5),
('PLT.SOL.RH', 7, 1, 0, 0, 3, 0.5),
('WIR.SS.08MM', 11.5, 1.2, 9.4, 0.9, 6.2, 0.6),
('CHM.HRT.SS', 450, 40, 0, 0, 120, 10),
('STR.LTH.BRN.20', 25, 0, 0, 0, 0, 0),
('PKG.BOX.XMAS', 0, 0, 0, 0, 0, 0)
) AS s(erp_id, wh110, fl110, wh125, fl125, wh315, fl315)
CROSS JOIN LATERAL (VALUES ('110', s.wh110, s.fl110), ('125', s.wh125, s.fl125), ('315', s.wh315, s.fl315)) AS f(factory_id, wh, fl);

UPDATE catalog.component_factory cf
SET critical_stock_level_override = o.level
FROM (VALUES
('R.WG.10K.BRD.08MM', '125', 5),
('CHN.VM.CABLE.45', '315', 450),
('STN.CZ.RND.3MM', '110', 4000),
('PKG.BOX.LOGO', '315', 1000)
) AS o(erp_id, factory_id, level)
WHERE cf.component_erp_id = o.erp_id AND cf.factory_id = o.factory_id;

UPDATE catalog.component_factory cf
SET manual_oos_active = true, manual_oos_set_by = o.set_by, manual_oos_set_at = now() - o.ago, manual_oos_note = o.note
FROM (VALUES
('R.WG.10K.BRD.08MM', '125', 'samir.haddad@tenengroup.com', interval '4 days 2 hours', 'Supply scheduled for Feb 1'),
('STN.DIA.RND.002', '125', 'samir.haddad@tenengroup.com', interval '6 days 5 hours', 'Batch failed QC — quarantined'),
('CLS.SPR.6MM.SS', '315', 'zsofia.nagy@tenengroup.com', interval '3 days 1 hour', 'Wrong size delivered')
) AS o(erp_id, factory_id, set_by, ago, note)
WHERE cf.component_erp_id = o.erp_id AND cf.factory_id = o.factory_id;

INSERT INTO catalog.component_alternative(component_erp_id, alternative_erp_id, for_balancer, for_planner, created_by, created_at, updated_at)
SELECT a.c, a.alt, a.bal, a.plan, 'maya.bendavid@tenengroup.com', now() - a.ago, now() - a.ago
FROM (VALUES
('R.WG.10K.BRD.08MM', 'R.WG.10K.BRD.09MM', true, true, interval '40 days'),
('CHN.VM.CABLE', 'CHN.VM.CABLE.45', true, false, interval '35 days'),
('CLS.LOB.10MM.VM', 'CLS.LOB.10MM.SS', false, true, interval '30 days'),
('JR.VM.5MM', 'JR.SS.5MM', false, true, interval '28 days'),
('PKG.POUCH.VEL', 'PKG.BOX.LOGO', true, true, interval '20 days'),
('STN.BRT.SAP.4MM', 'STN.BRT.GAR.4MM', false, true, interval '12 days'),
('STN.DIA.RND.002', 'STN.CZ.RND.3MM', false, true, interval '9 days')
) AS a(c, alt, bal, plan, ago);

INSERT INTO catalog.process(process_id, name, type, measured_in, description, status, created_at, updated_at)
SELECT v.process_id, v.name, v.type, v.unit, v.description, 'Active', now() - interval '90 days', now() - interval '30 days'
FROM (VALUES
('CUTTING', 'Laser cutting', 'Machine', 'Seconds', 'Cutting the name or shape out of the board'),
('ENGRAVING', 'Laser engraving', 'Machine', 'Seconds', 'Engraving text, coordinates or images'),
('CNC_MILLING', 'CNC milling', 'Machine', 'Minutes', 'Milling rings and 3D shapes'),
('POLISHING', 'Polishing', 'Machine', 'Minutes', 'Tumbling and hand polishing'),
('PLATING', 'Gold plating', 'Machine', 'Cm2', 'Gold plating of vermeil items'),
('RHODIUM', 'Rhodium plating', 'Machine', 'Cm2', 'Rhodium plating of white gold and silver items'),
('SETTING', 'Stone setting', 'Work', 'Pieces', 'Setting diamonds, zirconia and birthstones'),
('SOLDERING', 'Soldering', 'Work', 'Units', 'Soldering of links, posts and jump rings'),
('ASSEMBLY', 'Chain assembly', 'Work', 'Units', 'Attaching the chain, clasp and jump rings'),
('QC', 'Quality control', 'Work', 'Minutes', 'Final inspection before packing')
) AS v(process_id, name, type, unit, description);

INSERT INTO catalog.process_factory(process_id, factory_id, runs_here, machine_type, capacity_pct, capacity_reason, expected_recovery,
    capacity_updated_by, capacity_updated_at, max_per_day, labor_cost_amount, labor_cost_currency, labor_cost_effective_from, updated_at)
SELECT v.process_id, v.factory_id, v.runs_here, v.machine_type, 100, NULL, NULL, NULL, NULL,
       CASE WHEN v.runs_here THEN v.max_per_day END,
       CASE WHEN v.runs_here THEN v.labor END,
       CASE WHEN v.runs_here THEN 'USD' END,
       CASE WHEN v.runs_here THEN (now() - interval '60 days')::date END,
       now() - interval '30 days'
FROM (VALUES
('CUTTING', '110', true, 'Laser X2', 1000, 0.012),
('CUTTING', '125', true, 'Laser X2', 600, 0.011),
('CUTTING', '315', true, 'Laser X2 · Gen 2', 400, 0.009),
('ENGRAVING', '110', true, 'LASER', 900, 0.015),
('ENGRAVING', '125', true, 'LASER', 550, 0.014),
('ENGRAVING', '315', true, 'LASER', 380, 0.011),
('CNC_MILLING', '110', true, 'CNC', 300, 0.55),
('CNC_MILLING', '125', false, NULL, NULL, NULL),
('CNC_MILLING', '315', true, 'CNC', 160, 0.42),
('POLISHING', '110', true, 'Tumbler', 1200, 0.30),
('POLISHING', '125', true, 'Tumbler', 700, 0.28),
('POLISHING', '315', true, 'Tumbler', 450, 0.22),
('PLATING', '110', true, 'Plating line', 1000, 0.08),
('PLATING', '125', true, 'Plating line', 650, 0.075),
('PLATING', '315', true, 'Plating line', 420, 0.06),
('RHODIUM', '110', true, 'Plating line', 500, 0.12),
('RHODIUM', '125', true, 'Plating line', 300, 0.11),
('RHODIUM', '315', false, NULL, NULL, NULL),
('SETTING', '110', true, NULL, 400, 0.65),
('SETTING', '125', true, NULL, 260, 0.60),
('SETTING', '315', true, NULL, 180, 0.45),
('SOLDERING', '110', true, NULL, 600, 0.40),
('SOLDERING', '125', true, NULL, 380, 0.38),
('SOLDERING', '315', true, NULL, 250, 0.30),
('ASSEMBLY', '110', true, NULL, 1100, 0.35),
('ASSEMBLY', '125', true, NULL, 650, 0.33),
('ASSEMBLY', '315', true, NULL, 420, 0.26),
('QC', '110', true, NULL, 1200, 0.25),
('QC', '125', true, NULL, 700, 0.24),
('QC', '315', true, NULL, 450, 0.19),
('PACKING', '110', true, NULL, 1300, 0.45),
('PACKING', '125', true, NULL, 750, 0.42),
('PACKING', '315', true, NULL, 480, 0.35)
) AS v(process_id, factory_id, runs_here, machine_type, max_per_day, labor);

UPDATE catalog.process_factory pf
SET capacity_pct = c.pct, capacity_reason = c.reason, expected_recovery = (now() + c.recovery)::date,
    capacity_updated_by = c.by_user, capacity_updated_at = now() - c.ago, updated_at = now() - c.ago
FROM (VALUES
('ENGRAVING', '125', 0, 'Laser head down — awaiting spare part', interval '4 days', 'samir.haddad@tenengroup.com', interval '1 day 3 hours'),
('PLATING', '315', 60, 'Reduced staffing this week', interval '3 days', 'zsofia.nagy@tenengroup.com', interval '2 days 1 hour'),
('SETTING', '110', 80, 'Two setters on leave', interval '7 days', 'avi.levi@tenengroup.com', interval '3 days 4 hours'),
('POLISHING', '125', 40, 'One polishing line under maintenance', interval '2 days', 'samir.haddad@tenengroup.com', interval '20 hours')
) AS c(process_id, factory_id, pct, reason, recovery, by_user, ago)
WHERE pf.process_id = c.process_id AND pf.factory_id = c.factory_id;

INSERT INTO catalog.global_factory_share(factory_id, pct, updated_by, updated_at) VALUES
('110', 40, 'dana.ofer@tenengroup.com', now() - interval '30 days'),
('125', 35, 'dana.ofer@tenengroup.com', now() - interval '30 days'),
('315', 25, 'dana.ofer@tenengroup.com', now() - interval '30 days')
ON CONFLICT (factory_id) DO UPDATE SET pct = EXCLUDED.pct, updated_by = EXCLUDED.updated_by, updated_at = EXCLUDED.updated_at;

INSERT INTO catalog.sku(sku_uuid, site_sku, name, product_type, status, dropshipper_id, dropshipper_name,
    dropshipper_total_cost_amount, dropshipper_total_cost_currency, validation_status, validation_issues,
    last_master_sync_at, created_at, updated_at)
SELECT ('de300000-0000-4000-8000-0000000000' || v.n)::uuid, v.site_sku, v.name, v.product_type, v.status,
       v.ds_id, v.ds_name, v.ds_cost, CASE WHEN v.ds_cost IS NOT NULL THEN 'USD' END,
       CASE WHEN cardinality(v.issues) = 0 THEN 'Valid' ELSE 'Invalid' END, v.issues,
       now() - interval '2 hours', now() - interval '60 days', now() - interval '2 hours'
FROM (VALUES
('01', 'TGI-N-4609-SD-VMGP_1.1', 'Name necklace with diamond, gold vermeil', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('02', 'TGI-N-4610-SS_1.0', 'Name necklace, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('03', 'TGI-R-2201-WG10K_1.2', 'Name ring, 10K white gold', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('04', 'TGI-B-3105-VMGP_1.0', 'Engraved bar bracelet, gold vermeil', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('05', 'TGI-N-5120-BS-SS_1.0', 'Birthstone family necklace, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('06', 'TGI-N-5121-BS-VMGP_1.0', 'Birthstone family necklace, gold vermeil', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('07', 'TGI-E-0880-CZ-SS_1.0', 'Stud earrings with cubic zirconia, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('08', 'TGI-N-6001-YG14K_1.0', 'Script name necklace, 14K yellow gold', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('09', 'TGI-N-6002-RG14K_1.0', 'Script name necklace, 14K rose gold', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('10', 'TGI-N-7310-DIA-WG10K_1.0', 'Initial necklace with diamond, 10K white gold', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('11', 'TGI-B-3300-SS_1.1', 'Coordinates bar bracelet, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('12', 'TGI-N-1200-HRT-VMGP_1.0', 'Mini heart initial necklace, gold vermeil', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('13', 'TGI-N-4800-SS_1.0', 'Double name necklace, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('14', 'TGI-R-2250-SS_1.0', 'Stackable name ring, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('15', 'TGI-R-2310-BS-VMGP_1.0', 'Birthstone ring, gold vermeil', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('16', 'TGI-N-4609-SD-VMGP_1.0', 'Name necklace with diamond, gold vermeil (v1.0)', 'Manufactured', 'Retired', NULL, NULL, NULL, '{}'::text[]),
('17', 'TGI-N-9001-CHM-SS_1.0', 'Heart charm name necklace, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL,
    ARRAY['Component CHM.HRT.SS is not active in SAP']),
('18', 'TGI-E-0950-OPL-VMGP_1.0', 'Opal hoop earrings, gold vermeil', 'Manufactured', 'Active', NULL, NULL, NULL,
    ARRAY['Component STN.OPL.RND.3MM not found in SAP', 'Process ENAMELING not found in the catalog']),
('19', 'TGI-N-6100-WIR-SS_1.0', 'Wire name necklace, sterling silver', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('20', 'TGI-B-3400-VMGP_1.0', 'Charm bracelet, gold vermeil', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('21', 'TGI-E-0890-DIA-WG10K_1.0', 'Diamond stud earrings, 10K white gold', 'Manufactured', 'Active', NULL, NULL, NULL, '{}'::text[]),
('22', '110-12-4492-33', 'Leather watch strap', 'Dropshipper', 'Active', 'DS-STRAPWORKS', 'Strapworks', 18.40, '{}'::text[]),
('23', '110-09-7745-02', 'Custom photo print canvas', 'Dropshipper', 'Active', 'DS-PRINTPARTNER', 'Print Partner', 24.90, '{}'::text[]),
('24', 'DS-1001', 'Engraved wooden jewelry box', 'Dropshipper', 'Active', 'DS-LUMIERE', 'Lumière Supply Co.', 12.75, '{}'::text[])
) AS v(n, site_sku, name, product_type, status, ds_id, ds_name, ds_cost, issues);

INSERT INTO catalog.sku_alias(sku_uuid, alias, type, deprecated)
SELECT ('de300000-0000-4000-8000-0000000000' || v.n)::uuid, v.alias, 'Factory', true
FROM (VALUES
('01', 'TGN-N-4609-SD-VMGP_1.1'), ('01', 'TGO-N-4609-SD-VMGP_1.1'), ('02', 'TGN-N-4610-SS_1.0'),
('03', 'TGH-R-2201-WG10K_1.2'), ('04', 'TGN-B-3105-VMGP_1.0'), ('05', 'TGO-N-5120-BS-SS_1.0'),
('06', 'TGO-N-5121-BS-VMGP_1.0'), ('08', 'TGH-N-6001-YG14K_1.0'), ('10', 'TGN-N-7310-DIA-WG10K_1.0'),
('13', 'TGN-N-4800-SS_1.0'), ('16', 'TGN-N-4609-SD-VMGP_1.0')
) AS v(n, alias);

INSERT INTO catalog.sku_component(sku_uuid, component_erp_id, quantity, usage)
SELECT ('de300000-0000-4000-8000-0000000000' || v.n)::uuid, v.erp_id, v.qty, v.usage
FROM (VALUES
('01', 'R.VM.BRD.08MM', 0.004, 'Dynamic'), ('01', 'CHN.VM.CABLE.45', 1, 'Static'), ('01', 'STN.DIA.RND.002', 1, 'Static'),
('01', 'JR.VM.5MM', 2, 'Static'), ('01', 'PKG.BOX.LOGO', 1, 'Static'), ('01', 'PKG.CARD.CARE', 1, 'Static'),
('02', 'R.SS.BRD.08MM', 0.004, 'Dynamic'), ('02', 'CHN.SS.CABLE.45', 1, 'Static'), ('02', 'JR.SS.5MM', 2, 'Static'),
('02', 'PKG.POUCH.VEL', 1, 'Static'), ('02', 'PKG.CARD.CARE', 1, 'Static'),
('03', 'R.WG.10K.BRD.08MM', 0.003, 'Dynamic'), ('03', 'PKG.BOX.LOGO', 1, 'Static'), ('03', 'PKG.CARD.CARE', 1, 'Static'),
('04', 'R.VM.BRD.08MM', 0.002, 'Dynamic'), ('04', 'CHN.VM.CABLE', 1, 'Static'), ('04', 'CLS.LOB.10MM.VM', 1, 'Static'),
('04', 'JR.VM.5MM', 2, 'Static'), ('04', 'PKG.BOX.LOGO', 1, 'Static'),
('05', 'R.SS.BRD.08MM', 0.003, 'Dynamic'), ('05', 'CHN.SS.ROLO.50', 1, 'Static'), ('05', 'STN.BRT.GAR.4MM', 3, 'Dynamic'),
('05', 'JR.SS.5MM', 3, 'Static'), ('05', 'PKG.POUCH.VEL', 1, 'Static'),
('06', 'R.VM.BRD.08MM', 0.003, 'Dynamic'), ('06', 'CHN.VM.CABLE.45', 1, 'Static'), ('06', 'STN.BRT.SAP.4MM', 2, 'Dynamic'),
('06', 'STN.BRT.EME.4MM', 1, 'Dynamic'), ('06', 'JR.VM.5MM', 3, 'Static'), ('06', 'PKG.BOX.LOGO', 1, 'Static'),
('07', 'R.SS.BRD.08MM', 0.001, 'Dynamic'), ('07', 'STN.CZ.RND.3MM', 2, 'Static'), ('07', 'PKG.BOX.LOGO', 1, 'Static'),
('08', 'R.YG.14K.BRD.08MM', 0.003, 'Dynamic'), ('08', 'CHN.YG.14K.FIG.50', 1, 'Static'), ('08', 'PKG.BOX.LOGO', 1, 'Static'),
('08', 'PKG.CARD.CARE', 1, 'Static'),
('09', 'R.RG.14K.BRD.08MM', 0.003, 'Dynamic'), ('09', 'CHN.YG.14K.FIG.50', 1, 'Static'), ('09', 'PKG.BOX.LOGO', 1, 'Static'),
('10', 'R.WG.10K.BRD.08MM', 0.002, 'Dynamic'), ('10', 'CHN.WG.10K.BOX.45', 1, 'Static'), ('10', 'STN.DIA.RND.005', 1, 'Static'),
('10', 'PKG.BOX.LOGO', 1, 'Static'),
('11', 'R.SS.BRD.08MM', 0.002, 'Dynamic'), ('11', 'CHN.SS.CABLE.45', 1, 'Static'), ('11', 'CLS.SPR.6MM.SS', 1, 'Static'),
('11', 'JR.SS.5MM', 2, 'Static'), ('11', 'PKG.POUCH.VEL', 1, 'Static'),
('12', 'R.VM.BRD.08MM', 0.0015, 'Dynamic'), ('12', 'CHN.VM.CABLE', 1, 'Static'), ('12', 'JR.VM.5MM', 1, 'Static'),
('12', 'PKG.BOX.LOGO', 1, 'Static'),
('13', 'R.SS.BRD.08MM', 0.005, 'Dynamic'), ('13', 'CHN.SS.CABLE.45', 1, 'Static'), ('13', 'JR.SS.5MM', 4, 'Static'),
('13', 'PKG.POUCH.VEL', 1, 'Static'),
('14', 'R.SS.BRD.08MM', 0.002, 'Dynamic'), ('14', 'PKG.POUCH.VEL', 1, 'Static'),
('15', 'R.VM.BRD.08MM', 0.002, 'Dynamic'), ('15', 'STN.BRT.GAR.4MM', 1, 'Dynamic'), ('15', 'PKG.BOX.LOGO', 1, 'Static'),
('16', 'R.VM.BRD.08MM', 0.004, 'Dynamic'), ('16', 'CHN.VM.CABLE', 1, 'Static'), ('16', 'STN.DIA.RND.002', 1, 'Static'),
('16', 'PKG.BOX.LOGO', 1, 'Static'),
('17', 'R.SS.BRD.08MM', 0.003, 'Dynamic'), ('17', 'CHN.SS.CABLE.45', 1, 'Static'), ('17', 'CHM.HRT.SS', 1, 'Static'),
('17', 'JR.SS.5MM', 2, 'Static'),
('18', 'R.VM.BRD.08MM', 0.002, 'Dynamic'), ('18', 'STN.OPL.RND.3MM', 2, 'Static'), ('18', 'PKG.BOX.LOGO', 1, 'Static'),
('19', 'WIR.SS.08MM', 0.003, 'Dynamic'), ('19', 'CHN.SS.CABLE.45', 1, 'Static'), ('19', 'JR.SS.5MM', 2, 'Static'),
('19', 'PKG.POUCH.VEL', 1, 'Static'),
('20', 'CHN.VM.CABLE', 1, 'Static'), ('20', 'CLS.LOB.10MM.VM', 1, 'Static'), ('20', 'JR.VM.5MM', 4, 'Static'),
('20', 'PKG.BOX.LOGO', 1, 'Static'),
('21', 'R.WG.10K.BRD.08MM', 0.001, 'Dynamic'), ('21', 'STN.DIA.RND.002', 2, 'Static'), ('21', 'PKG.BOX.LOGO', 1, 'Static')
) AS v(n, erp_id, qty, usage);

INSERT INTO catalog.sku_process(sku_uuid, process_id, quantity)
SELECT ('de300000-0000-4000-8000-0000000000' || v.n)::uuid, v.process_id, v.qty
FROM (VALUES
('01', 'CUTTING', 45), ('01', 'POLISHING', 3), ('01', 'PLATING', 12.5), ('01', 'SETTING', 1), ('01', 'ASSEMBLY', 1), ('01', 'QC', 2), ('01', 'PACKING', 1),
('02', 'CUTTING', 40), ('02', 'POLISHING', 3), ('02', 'RHODIUM', 10), ('02', 'ASSEMBLY', 1), ('02', 'QC', 1.5), ('02', 'PACKING', 1),
('03', 'CNC_MILLING', 6), ('03', 'POLISHING', 4), ('03', 'RHODIUM', 8), ('03', 'ENGRAVING', 20), ('03', 'QC', 2), ('03', 'PACKING', 1),
('04', 'CUTTING', 30), ('04', 'ENGRAVING', 25), ('04', 'POLISHING', 2), ('04', 'PLATING', 8), ('04', 'ASSEMBLY', 1), ('04', 'QC', 1), ('04', 'PACKING', 1),
('05', 'CUTTING', 35), ('05', 'POLISHING', 3), ('05', 'SETTING', 3), ('05', 'ASSEMBLY', 1), ('05', 'QC', 2), ('05', 'PACKING', 1),
('06', 'CUTTING', 35), ('06', 'POLISHING', 3), ('06', 'PLATING', 10), ('06', 'SETTING', 3), ('06', 'ASSEMBLY', 1), ('06', 'QC', 2), ('06', 'PACKING', 1),
('07', 'CUTTING', 15), ('07', 'POLISHING', 2), ('07', 'SETTING', 2), ('07', 'SOLDERING', 2), ('07', 'QC', 1), ('07', 'PACKING', 1),
('08', 'CUTTING', 50), ('08', 'POLISHING', 4), ('08', 'SOLDERING', 1), ('08', 'QC', 3), ('08', 'PACKING', 1),
('09', 'CUTTING', 50), ('09', 'POLISHING', 4), ('09', 'SOLDERING', 1), ('09', 'QC', 3), ('09', 'PACKING', 1),
('10', 'CUTTING', 25), ('10', 'POLISHING', 3), ('10', 'RHODIUM', 6), ('10', 'SETTING', 1), ('10', 'ASSEMBLY', 1), ('10', 'QC', 3), ('10', 'PACKING', 1),
('11', 'CUTTING', 30), ('11', 'ENGRAVING', 35), ('11', 'POLISHING', 2), ('11', 'ASSEMBLY', 1), ('11', 'QC', 1), ('11', 'PACKING', 1),
('12', 'CUTTING', 20), ('12', 'POLISHING', 2), ('12', 'PLATING', 6), ('12', 'ASSEMBLY', 1), ('12', 'QC', 1), ('12', 'PACKING', 1),
('13', 'CUTTING', 70), ('13', 'POLISHING', 4), ('13', 'SOLDERING', 2), ('13', 'ASSEMBLY', 1), ('13', 'QC', 2), ('13', 'PACKING', 1),
('14', 'CNC_MILLING', 5), ('14', 'POLISHING', 3), ('14', 'ENGRAVING', 15), ('14', 'QC', 1), ('14', 'PACKING', 1),
('15', 'CNC_MILLING', 5), ('15', 'POLISHING', 3), ('15', 'PLATING', 5), ('15', 'SETTING', 1), ('15', 'QC', 1), ('15', 'PACKING', 1),
('16', 'CUTTING', 45), ('16', 'POLISHING', 3), ('16', 'PLATING', 12.5), ('16', 'SETTING', 1), ('16', 'PACKING', 1),
('17', 'CUTTING', 40), ('17', 'ASSEMBLY', 2), ('17', 'PACKING', 1),
('18', 'CUTTING', 20), ('18', 'ENAMELING', 4), ('18', 'PACKING', 1),
('19', 'SOLDERING', 3), ('19', 'POLISHING', 2), ('19', 'ASSEMBLY', 1), ('19', 'QC', 1), ('19', 'PACKING', 1),
('20', 'ASSEMBLY', 2), ('20', 'PLATING', 4), ('20', 'QC', 1), ('20', 'PACKING', 1),
('21', 'CUTTING', 12), ('21', 'POLISHING', 2), ('21', 'RHODIUM', 3), ('21', 'SETTING', 2), ('21', 'SOLDERING', 2), ('21', 'QC', 2), ('21', 'PACKING', 1)
) AS v(n, process_id, qty);

INSERT INTO catalog.sku_factory(sku_uuid, factory_id, producible, override_share_pct, override_by, override_at, updated_at)
SELECT s.sku_uuid, f.id,
       s.product_type = 'Manufactured'
         AND NOT EXISTS (
           SELECT 1 FROM catalog.sku_process sp
           LEFT JOIN catalog.process_factory pf ON pf.process_id = sp.process_id AND pf.factory_id = f.id
           JOIN catalog.process p ON p.process_id = sp.process_id
           WHERE sp.sku_uuid = s.sku_uuid AND pf.runs_here IS NOT TRUE),
       NULL, NULL, NULL, now() - interval '2 hours'
FROM catalog.sku s
CROSS JOIN catalog.factory f
WHERE s.sku_uuid::text LIKE 'de300000-%' AND f.id IN ('110', '125', '315');

UPDATE catalog.sku_factory sf
SET override_share_pct = o.pct, override_by = o.by_user, override_at = now() - o.ago, updated_at = now() - o.ago
FROM (VALUES
('05', '125', 50, 'samir.haddad@tenengroup.com', interval '5 days'),
('07', '315', 40, 'zsofia.nagy@tenengroup.com', interval '8 days'),
('13', '110', 20, 'avi.levi@tenengroup.com', interval '2 days')
) AS o(n, factory_id, pct, by_user, ago)
WHERE sf.sku_uuid = ('de300000-0000-4000-8000-0000000000' || o.n)::uuid AND sf.factory_id = o.factory_id;

INSERT INTO catalog.history_entry(entity_type, entity_id, factory_id, field, old_value, new_value, changed_by, changed_at, source)
SELECT h.entity_type, h.entity_id, h.factory_id, h.field, h.old_value, h.new_value, h.changed_by, now() - h.ago, h.source
FROM (VALUES
('component', 'R.WG.10K.BRD.08MM', NULL, 'criticalStockLevel', '1.5', '2', 'maya.bendavid@tenengroup.com', interval '25 days', 'User'),
('component', 'R.WG.10K.BRD.08MM', '125', 'criticalStockLevelOverride', NULL, '5', 'maya.bendavid@tenengroup.com', interval '25 days', 'User'),
('component', 'R.WG.10K.BRD.08MM', NULL, 'alternative', NULL, 'R.WG.10K.BRD.09MM', 'maya.bendavid@tenengroup.com', interval '40 days', 'User'),
('component', 'R.WG.10K.BRD.08MM', NULL, 'unitCostAmount', '238.1', '245.5', 'SAP sync', interval '9 days', 'Sync'),
('component', 'R.WG.10K.BRD.08MM', '125', 'manualOos', 'false', 'true', 'samir.haddad@tenengroup.com', interval '4 days 2 hours', 'User'),
('component', 'R.WG.10K.BRD.08MM', '125', 'manualOosNote', NULL, 'Supply scheduled for Feb 1', 'samir.haddad@tenengroup.com', interval '4 days 2 hours', 'User'),
('component', 'STN.DIA.RND.002', '125', 'manualOos', 'false', 'true', 'samir.haddad@tenengroup.com', interval '6 days 5 hours', 'User'),
('component', 'STN.DIA.RND.002', '125', 'manualOosNote', NULL, 'Batch failed QC — quarantined', 'samir.haddad@tenengroup.com', interval '6 days 5 hours', 'User'),
('component', 'CLS.SPR.6MM.SS', '315', 'manualOos', 'false', 'true', 'zsofia.nagy@tenengroup.com', interval '3 days 1 hour', 'User'),
('component', 'CLS.SPR.6MM.SS', '315', 'manualOosNote', NULL, 'Wrong size delivered', 'zsofia.nagy@tenengroup.com', interval '3 days 1 hour', 'User'),
('component', 'CHM.HRT.SS', NULL, 'status', 'Active', 'Inactive', 'SAP sync', interval '11 days', 'Sync'),
('process', 'ENGRAVING', '125', 'capacityPct', '100%', '0% (reason: Laser head down — awaiting spare part)', 'samir.haddad@tenengroup.com', interval '1 day 3 hours', 'User'),
('process', 'ENGRAVING', '125', 'capacityReason', NULL, 'Laser head down — awaiting spare part', 'samir.haddad@tenengroup.com', interval '1 day 3 hours', 'User'),
('process', 'PLATING', '315', 'capacityPct', '100%', '60% (reason: Reduced staffing this week)', 'zsofia.nagy@tenengroup.com', interval '2 days 1 hour', 'User'),
('process', 'PLATING', '315', 'capacityReason', NULL, 'Reduced staffing this week', 'zsofia.nagy@tenengroup.com', interval '2 days 1 hour', 'User'),
('process', 'SETTING', '110', 'capacityPct', '100%', '80% (reason: Two setters on leave)', 'avi.levi@tenengroup.com', interval '3 days 4 hours', 'User'),
('process', 'POLISHING', '125', 'capacityPct', '70%', '40% (reason: One polishing line under maintenance)', 'samir.haddad@tenengroup.com', interval '20 hours', 'User'),
('process', 'CUTTING', '315', 'laborCostAmount', '0.0085', '0.009', 'dana.ofer@tenengroup.com', interval '60 days', 'User'),
('process', 'CUTTING', '315', 'machineType', 'Laser X2', 'Laser X2 · Gen 2', 'dana.ofer@tenengroup.com', interval '45 days', 'User'),
('sku', 'de300000-0000-4000-8000-000000000005', '125', 'overrideSharePct', NULL, '50', 'samir.haddad@tenengroup.com', interval '5 days', 'User'),
('sku', 'de300000-0000-4000-8000-000000000007', '315', 'overrideSharePct', NULL, '40', 'zsofia.nagy@tenengroup.com', interval '8 days', 'User'),
('sku', 'de300000-0000-4000-8000-000000000013', '110', 'overrideSharePct', '35', '20', 'avi.levi@tenengroup.com', interval '2 days', 'User'),
('sku', 'de300000-0000-4000-8000-000000000017', NULL, 'validationStatus', 'Valid', 'Invalid', 'Master catalog sync', interval '11 days', 'Sync'),
('sku', 'de300000-0000-4000-8000-000000000017', NULL, 'validationIssues', '', 'Component CHM.HRT.SS is not active in SAP', 'Master catalog sync', interval '11 days', 'Sync'),
('global-factory-share', 'global', '110', 'pct', '33.33', '40', 'dana.ofer@tenengroup.com', interval '30 days', 'User'),
('global-factory-share', 'global', '125', 'pct', '33.33', '35', 'dana.ofer@tenengroup.com', interval '30 days', 'User'),
('global-factory-share', 'global', '315', 'pct', '33.34', '25', 'dana.ofer@tenengroup.com', interval '30 days', 'User')
) AS h(entity_type, entity_id, factory_id, field, old_value, new_value, changed_by, ago, source);

INSERT INTO balancer.shipping_route_cache(origin_facility_id, destination_country, cost_amount, cost_currency, transit_days_min, transit_days_max, refreshed_at)
SELECT r.origin, r.country, r.cost, 'USD', r.tmin, r.tmax, least(now(), date_trunc('day', now()) + interval '1 hour')
FROM (VALUES
('110', 'US', 9.80, 4, 6), ('110', 'GB', 8.40, 3, 5), ('110', 'DE', 7.90, 3, 5), ('110', 'IL', 3.20, 1, 2),
('110', 'AU', 12.60, 6, 9), ('110', 'CA', 10.40, 5, 7), ('110', 'FR', 7.95, 3, 5),
('125', 'US', 9.60, 4, 6), ('125', 'GB', 8.30, 3, 5), ('125', 'DE', 7.80, 3, 5), ('125', 'IL', 2.90, 1, 2),
('125', 'AU', 12.90, 6, 9), ('125', 'CA', 10.20, 5, 7), ('125', 'FR', 7.85, 3, 5),
('315', 'US', 11.20, 5, 7), ('315', 'GB', 6.10, 2, 4), ('315', 'DE', 4.80, 2, 3), ('315', 'IL', 7.40, 3, 5),
('315', 'AU', 14.10, 7, 10), ('315', 'CA', 12.30, 6, 8), ('315', 'FR', 5.20, 2, 3)
) AS r(origin, country, cost, tmin, tmax)
ON CONFLICT (origin_facility_id, destination_country) DO UPDATE
SET cost_amount = EXCLUDED.cost_amount, cost_currency = EXCLUDED.cost_currency, transit_days_min = EXCLUDED.transit_days_min,
    transit_days_max = EXCLUDED.transit_days_max, refreshed_at = EXCLUDED.refreshed_at;

INSERT INTO balancer.balancer_settings(version, mode, late_penalty_per_day, split_penalty, share_weight, success_rate_enabled,
    success_rate_weight, share_window_days, share_gap_threshold_pct, default_production_days, updated_by, updated_at)
VALUES (1, 'Live', 5, 3, 0.1, false, 0.5, 7, 2, 3, 'ravit.tal@tenengroup.com', now() - interval '15 days')
ON CONFLICT (version) DO NOTHING;

INSERT INTO balancer.capacity_reservation(factory_id, date_from, date_to, reserved_pct, event_name, updated_by, updated_at) VALUES
('110', (now() + interval '3 days')::date, (now() + interval '4 days')::date, 20, 'Black Friday', 'ravit.tal@tenengroup.com', now() - interval '6 days'),
('125', (now() + interval '21 days')::date, (now() + interval '26 days')::date, 15, 'Christmas', 'ravit.tal@tenengroup.com', now() - interval '2 days');

CREATE FUNCTION pg_temp.demo_stock_status(p_erp text, p_factory text) RETURNS text LANGUAGE sql AS $f$
    SELECT CASE
        WHEN cf.component_erp_id IS NULL THEN 'unknown'
        WHEN cf.manual_oos_active OR cf.warehouse_stock + cf.production_floor_stock = 0 THEN 'outOfStock'
        WHEN cf.warehouse_stock + cf.production_floor_stock < coalesce(cf.critical_stock_level_override, c.critical_stock_level, 0) THEN 'low'
        ELSE 'ok' END
    FROM (SELECT 1) AS one
    LEFT JOIN catalog.component_factory cf ON cf.component_erp_id = p_erp AND cf.factory_id = p_factory
    LEFT JOIN catalog.component c ON c.erp_id = p_erp
$f$;

CREATE FUNCTION pg_temp.demo_quote(p_sku uuid, p_factory text)
RETURNS TABLE(o_producible boolean, o_reasons text[], o_material numeric, o_labor numeric, o_packing numeric,
              o_capacity int, o_components jsonb, o_processes jsonb)
LANGUAGE plpgsql AS $f$
DECLARE
    r record;
    v_status text;
    v_alt text;
    v_alt_cost numeric;
    v_unit numeric;
    v_cost numeric;
    v_reasons text[] := '{}';
    v_components jsonb := '[]';
    v_processes jsonb := '[]';
    v_material numeric := 0;
    v_labor numeric := 0;
    v_packing numeric := 0;
    v_capacity int := 100;
BEGIN
    IF NOT coalesce((SELECT sf.producible FROM catalog.sku_factory sf WHERE sf.sku_uuid = p_sku AND sf.factory_id = p_factory), false) THEN
        v_reasons := array_append(v_reasons, 'NOT_PRODUCIBLE');
    END IF;

    FOR r IN
        SELECT sc.component_erp_id AS erp, sc.quantity AS qty, c.unit_cost_amount AS unit_cost
        FROM catalog.sku_component sc
        LEFT JOIN catalog.component c ON c.erp_id = sc.component_erp_id
        WHERE sc.sku_uuid = p_sku
        ORDER BY sc.component_erp_id
    LOOP
        v_status := pg_temp.demo_stock_status(r.erp, p_factory);
        v_alt := NULL;
        v_unit := r.unit_cost;
        IF v_status = 'unknown' THEN
            v_reasons := array_append(v_reasons, 'UNKNOWN_COMPONENT');
        ELSIF v_status = 'outOfStock' THEN
            SELECT a.alternative_erp_id, c2.unit_cost_amount INTO v_alt, v_alt_cost
            FROM catalog.component_alternative a
            JOIN catalog.component c2 ON c2.erp_id = a.alternative_erp_id
            WHERE a.component_erp_id = r.erp AND a.for_balancer
              AND pg_temp.demo_stock_status(a.alternative_erp_id, p_factory) IN ('ok', 'low')
            ORDER BY a.alternative_erp_id
            LIMIT 1;
            IF v_alt IS NULL THEN
                v_reasons := array_append(v_reasons, 'COMPONENT_OOS');
            ELSE
                v_unit := v_alt_cost;
                v_status := pg_temp.demo_stock_status(v_alt, p_factory);
            END IF;
        END IF;
        v_cost := round(r.qty * v_unit, 4);
        v_material := v_material + coalesce(v_cost, 0);
        v_components := v_components || jsonb_build_array(jsonb_build_object(
            'erpId', r.erp, 'quantity', r.qty, 'unitCost', v_unit, 'stockStatus', v_status,
            'alternativeUsed', v_alt, 'cost', v_cost, 'currency', CASE WHEN v_unit IS NOT NULL THEN 'USD' END));
    END LOOP;

    FOR r IN
        SELECT sp.process_id AS pid, sp.quantity AS qty, pf.runs_here, pf.capacity_pct AS cap,
               pf.labor_cost_amount AS labor, pf.labor_cost_currency AS currency
        FROM catalog.sku_process sp
        LEFT JOIN catalog.process_factory pf ON pf.process_id = sp.process_id AND pf.factory_id = p_factory
        WHERE sp.sku_uuid = p_sku
        ORDER BY sp.process_id
    LOOP
        IF r.runs_here IS NOT TRUE THEN
            v_reasons := array_append(v_reasons, 'PROCESS_NOT_AVAILABLE');
        ELSIF r.cap = 0 THEN
            v_reasons := array_append(v_reasons, 'PROCESS_OFFLINE');
        END IF;
        IF r.runs_here THEN
            v_capacity := least(v_capacity, r.cap);
        END IF;
        v_cost := round(r.qty * r.labor, 4);
        IF r.pid = 'PACKING' THEN
            v_packing := v_packing + coalesce(v_cost, 0);
        ELSE
            v_labor := v_labor + coalesce(v_cost, 0);
        END IF;
        v_processes := v_processes || jsonb_build_array(jsonb_build_object(
            'processId', r.pid, 'quantity', r.qty, 'capacityPct', r.cap, 'laborCost', v_cost,
            'remainingPct', r.cap, 'currency', r.currency));
    END LOOP;

    v_reasons := ARRAY(SELECT DISTINCT x FROM unnest(v_reasons) AS x ORDER BY x);

    RETURN QUERY SELECT cardinality(v_reasons) = 0, v_reasons,
        CASE WHEN cardinality(v_reasons) = 0 THEN round(v_material, 4) END,
        CASE WHEN cardinality(v_reasons) = 0 THEN round(v_labor, 4) END,
        CASE WHEN cardinality(v_reasons) = 0 THEN round(v_packing, 4) END,
        CASE WHEN cardinality(v_reasons) = 0 THEN v_capacity END,
        v_components, v_processes;
END
$f$;

CREATE FUNCTION pg_temp.demo_quote_set(p_key text, p_order text, p_item text, p_sku uuid, p_mode text, p_created timestamptz)
RETURNS bigint LANGUAGE plpgsql AS $f$
DECLARE
    v_id bigint;
    v_factory text;
    d record;
BEGIN
    INSERT INTO catalog.quote_set(quote_key, request_id, order_id, order_item_id, sku_uuid, quantity, mode, cbd, valid_until, created_at)
    VALUES (p_key, md5('demo-request-' || p_key)::uuid, p_order, p_item, p_sku, 1, p_mode,
            coalesce((SELECT jsonb_agg(jsonb_build_object('erpId', sc.component_erp_id, 'quantity', sc.quantity) ORDER BY sc.component_erp_id)
                      FROM catalog.sku_component sc WHERE sc.sku_uuid = p_sku), '[]'::jsonb),
            p_created + interval '30 minutes', p_created)
    RETURNING id INTO v_id;

    FOREACH v_factory IN ARRAY ARRAY['110', '125', '315'] LOOP
        SELECT * INTO d FROM pg_temp.demo_quote(p_sku, v_factory);
        INSERT INTO catalog.quote(quote_id, quote_set_id, factory_id, producible, reasons, material_cost, labor_cost, packing_cost,
            total_cost, currency, capacity_signal_pct, share_pct, components, processes, status, created_at)
        VALUES (p_key || '-' || v_factory, v_id, v_factory, d.o_producible, d.o_reasons, d.o_material, d.o_labor, d.o_packing,
            d.o_material + d.o_labor + d.o_packing, CASE WHEN d.o_producible THEN 'USD' END, d.o_capacity, 0,
            d.o_components, d.o_processes, 'Quoted', p_created);
    END LOOP;

    UPDATE catalog.quote q
    SET share_pct = s.pct
    FROM (
        SELECT b.id, round(b.base * 100 / nullif(sum(b.base) OVER (), 0), 2) AS pct
        FROM (
            SELECT q2.id, coalesce(sf.override_share_pct, g.pct, 0) AS base
            FROM catalog.quote q2
            LEFT JOIN catalog.sku_factory sf ON sf.sku_uuid = p_sku AND sf.factory_id = q2.factory_id
            LEFT JOIN catalog.global_factory_share g ON g.factory_id = q2.factory_id
            WHERE q2.quote_set_id = v_id AND q2.producible
        ) AS b
    ) AS s
    WHERE q.id = s.id AND s.pct IS NOT NULL;

    RETURN v_id;
END
$f$;

CREATE FUNCTION pg_temp.demo_production_log(p_sku uuid, p_factory text) RETURNS jsonb LANGUAGE sql AS $f$
    SELECT coalesce(jsonb_agg(jsonb_build_object('processId', sp.process_id, 'machineType', pf.machine_type) ORDER BY sp.process_id), '[]'::jsonb)
    FROM catalog.sku_process sp
    JOIN catalog.process_factory pf ON pf.process_id = sp.process_id AND pf.factory_id = p_factory
    WHERE sp.sku_uuid = p_sku
$f$;

DO $$
DECLARE
    v_now timestamptz := now();
    v_today date := (now() AT TIME ZONE 'UTC')::date;
    v_day_start timestamptz := ((now() AT TIME ZONE 'UTC')::date)::timestamp AT TIME ZONE 'UTC';
    v_skus uuid[] := ARRAY[
        'de300000-0000-4000-8000-000000000001', 'de300000-0000-4000-8000-000000000002', 'de300000-0000-4000-8000-000000000003',
        'de300000-0000-4000-8000-000000000004', 'de300000-0000-4000-8000-000000000005', 'de300000-0000-4000-8000-000000000006',
        'de300000-0000-4000-8000-000000000007', 'de300000-0000-4000-8000-000000000008', 'de300000-0000-4000-8000-000000000009',
        'de300000-0000-4000-8000-000000000010', 'de300000-0000-4000-8000-000000000011', 'de300000-0000-4000-8000-000000000012',
        'de300000-0000-4000-8000-000000000013', 'de300000-0000-4000-8000-000000000014', 'de300000-0000-4000-8000-000000000015',
        'de300000-0000-4000-8000-000000000019', 'de300000-0000-4000-8000-000000000020', 'de300000-0000-4000-8000-000000000021']::uuid[];
    v_countries text[] := ARRAY['US', 'GB', 'DE', 'US', 'IL', 'AU', 'CA', 'FR'];
    v_sites text[] := ARRAY['Myka', 'OAL', 'SETT', 'L&L'];
    v_pages_base jsonb := '{"110": 2140, "125": 3310, "315": 4025}';
    v_managers jsonb := '{"110": "avi.levi@tenengroup.com", "125": "samir.haddad@tenengroup.com", "315": "zsofia.nagy@tenengroup.com"}';
    i int;
    k int;
    v_order text;
    v_item text;
    v_sku uuid;
    v_mode text;
    v_fallback text;
    v_decided timestamptz;
    v_decided_date date;
    v_country text;
    v_pref text;
    v_event boolean;
    v_status text;
    v_due date;
    v_produced timestamptz;
    v_promised date;
    v_sla date;
    v_key text;
    v_set_id bigint;
    v_key2 text;
    v_set2_id bigint;
    v_winner text;
    v_to text;
    v_transferred_at timestamptz;
    v_final_factory text;
    v_final_key text;
    v_final_quote record;
    v_old_quote record;
    v_route record;
    v_old_route record;
    q record;
    v_cands jsonb;
    v_excluded text;
    v_ship numeric;
    v_total numeric;
    v_eta_from date;
    v_eta_to date;
    v_eff numeric;
    v_feasible int;
    v_cheapest numeric;
    v_cheapest_factory text;
    v_winner_total numeric;
    v_winner_meets boolean;
    v_reason text;
    v_alloc_id uuid;
    v_actual_labor numeric;
    v_line jsonb;
    v_record_id bigint;
    v_rep_quote_id bigint;
    v_rep_produced timestamptz;
    v_page text;
BEGIN
    FOR i IN 1..48 LOOP
        v_order := 'ORD' || (884300 + i);
        v_item := 'ITM001';
        IF i % 7 = 0 THEN
            v_order := 'ORD' || (884300 + i - 1);
            v_item := 'ITM002';
        END IF;
        v_sku := v_skus[1 + (i * 5) % cardinality(v_skus)];
        v_mode := CASE WHEN i <= 10 THEN 'Shadow' WHEN i IN (17, 29, 38, 45) THEN 'Fallback' ELSE 'Live' END;
        v_fallback := CASE WHEN i IN (17, 29, 38) THEN 'NoFeasibleFactory' WHEN i = 45 THEN 'RoutesUnavailable' END;
        v_decided := v_now - (48 - i) * interval '7 hours' - interval '25 minutes';
        v_decided_date := (v_decided AT TIME ZONE 'UTC')::date;
        v_country := v_countries[1 + i % 8];
        v_pref := CASE WHEN (i * 7) % 20 < 8 THEN '110' WHEN (i * 7) % 20 < 15 THEN '125' ELSE '315' END;
        IF i BETWEEN 37 AND 42 THEN
            v_pref := '110';
        END IF;
        v_event := i IN (39, 46);
        v_status := CASE WHEN v_mode = 'Shadow' THEN 'Allocated' WHEN i <= 36 THEN 'Produced' ELSE 'Allocated' END;
        v_due := CASE WHEN i BETWEEN 37 AND 41 THEN v_today WHEN i = 42 THEN v_today - 1 ELSE v_decided_date + 3 END;
        v_produced := CASE
            WHEN v_status <> 'Produced' THEN NULL
            WHEN i BETWEEN 31 AND 36 THEN v_day_start + (v_now - v_day_start) * (0.15 + (i - 31) * 0.13)
            WHEN i % 9 = 0 THEN least(v_decided + interval '4 days 2 hours', v_now - interval '1 hour')
            ELSE v_decided + interval '2 days' + (i % 3) * interval '5 hours' END;
        v_promised := v_decided_date + 10;
        v_sla := CASE WHEN i % 11 = 0 THEN v_decided_date + 12 END;

        v_key := 'PQ-' || v_order || '-' || v_item;
        v_set_id := pg_temp.demo_quote_set(v_key, v_order, v_item, v_sku,
            CASE WHEN v_mode = 'Shadow' THEN 'Shadow' ELSE 'Live' END, v_decided - interval '40 seconds');

        SELECT factory_id INTO v_winner FROM catalog.quote
        WHERE quote_set_id = v_set_id AND producible
        ORDER BY (factory_id = v_pref) DESC, factory_id
        LIMIT 1;

        v_to := NULL;
        IF i IN (28, 44, 47) THEN
            SELECT factory_id INTO v_to FROM catalog.quote
            WHERE quote_set_id = v_set_id AND producible AND factory_id <> v_winner
            ORDER BY factory_id
            LIMIT 1;
        END IF;

        v_cands := '[]';
        v_feasible := 0;
        v_cheapest := NULL;
        v_cheapest_factory := NULL;
        FOR q IN
            SELECT qt.*, r.cost_amount, r.transit_days_min, r.transit_days_max
            FROM catalog.quote qt
            LEFT JOIN balancer.shipping_route_cache r ON r.origin_facility_id = qt.factory_id AND r.destination_country = v_country
            WHERE qt.quote_set_id = v_set_id
            ORDER BY qt.factory_id
        LOOP
            v_excluded := CASE
                WHEN NOT q.producible THEN q.reasons[1]
                WHEN v_fallback = 'RoutesUnavailable' THEN 'NO_ROUTE'
                WHEN v_fallback = 'NoFeasibleFactory' THEN CASE q.factory_id WHEN '110' THEN 'CAPACITY_RESERVED' WHEN '125' THEN 'NO_CAPACITY' ELSE 'NO_ROUTE' END
                END;
            v_ship := CASE WHEN v_fallback = 'RoutesUnavailable' OR NOT q.producible THEN NULL ELSE q.cost_amount END;
            v_total := CASE WHEN q.producible THEN q.total_cost + coalesce(v_ship, 0) END;
            v_eta_from := CASE WHEN q.producible AND v_ship IS NOT NULL THEN v_decided_date + 3 + q.transit_days_min END;
            v_eta_to := CASE WHEN q.producible AND v_ship IS NOT NULL THEN v_decided_date + 3 + q.transit_days_max END;
            v_eff := CASE WHEN q.producible THEN round(q.share_pct * q.capacity_signal_pct / 100.0, 2) END;
            IF v_excluded IS NULL THEN
                v_feasible := v_feasible + 1;
                IF v_cheapest IS NULL OR v_total < v_cheapest THEN
                    v_cheapest := v_total;
                    v_cheapest_factory := q.factory_id;
                END IF;
            END IF;
            IF q.factory_id = v_winner THEN
                v_winner_total := v_total;
                v_winner_meets := v_eta_to <= v_promised;
            END IF;
            v_cands := v_cands || jsonb_build_array(jsonb_build_object(
                'factoryId', q.factory_id,
                'quoteId', q.quote_id,
                'productionCost', q.total_cost,
                'shippingCost', v_ship,
                'totalCost', v_total,
                'currency', CASE WHEN q.producible THEN 'USD' END,
                'etaFrom', v_eta_from,
                'etaTo', v_eta_to,
                'transitDaysMax', CASE WHEN v_ship IS NOT NULL THEN q.transit_days_max END,
                'sharePct', q.share_pct,
                'capacityPct', q.capacity_signal_pct,
                'effectiveSharePct', v_eff,
                'score', CASE WHEN v_excluded IS NULL THEN round(v_total * (1 - 0.1 * (v_eff - q.share_pct) / 100), 4) END,
                'meetsPromise', CASE WHEN v_eta_to IS NOT NULL THEN v_eta_to <= v_promised END,
                'meetsSla', CASE WHEN v_eta_to IS NOT NULL AND v_sla IS NOT NULL THEN v_eta_to <= v_sla END,
                'excludedReason', v_excluded,
                'isWinner', q.factory_id = v_winner));
        END LOOP;

        v_reason := CASE
            WHEN v_fallback IS NOT NULL THEN 'Fallback by default factory share (' || v_fallback || ')'
            WHEN v_feasible = 1 THEN 'Only feasible factory'
            WHEN v_winner_total = v_cheapest AND v_winner_meets THEN 'Lowest total cost that meets the delivery promise'
            WHEN v_winner_total = v_cheapest THEN 'Lowest total cost; the delivery promise is missed'
            ELSE 'Share, capacity and penalty adjustments outweigh the cost advantage of ' || v_cheapest_factory END;

        v_final_factory := v_winner;
        v_final_key := v_key;
        IF v_mode <> 'Shadow' THEN
            IF v_to IS NULL THEN
                UPDATE catalog.quote
                SET status = CASE WHEN v_status = 'Produced' THEN 'Produced' ELSE 'Selected' END,
                    selected_at = v_decided,
                    produced_at = v_produced,
                    production_log = CASE WHEN v_status = 'Produced' THEN pg_temp.demo_production_log(v_sku, v_winner) END
                WHERE quote_set_id = v_set_id AND factory_id = v_winner;
            ELSE
                v_transferred_at := CASE WHEN v_status = 'Produced' THEN v_decided + interval '1 day' ELSE least(v_decided + interval '6 hours', v_now - interval '10 minutes') END;
                UPDATE catalog.quote SET status = 'Transferred', selected_at = v_decided, transferred_at = v_transferred_at
                WHERE quote_set_id = v_set_id AND factory_id = v_winner;
                v_key2 := v_key || '-R2';
                v_set2_id := pg_temp.demo_quote_set(v_key2, v_order, v_item, v_sku, 'Live', v_transferred_at - interval '20 seconds');
                UPDATE catalog.quote
                SET status = CASE WHEN v_status = 'Produced' THEN 'Produced' ELSE 'Selected' END,
                    selected_at = v_transferred_at,
                    produced_at = v_produced,
                    production_log = CASE WHEN v_status = 'Produced' THEN pg_temp.demo_production_log(v_sku, v_to) END
                WHERE quote_set_id = v_set2_id AND factory_id = v_to;
                v_final_factory := v_to;
                v_final_key := v_key2;
            END IF;
        END IF;

        SELECT * INTO v_final_quote FROM catalog.quote WHERE quote_id = v_final_key || '-' || v_final_factory;
        SELECT * INTO v_route FROM balancer.shipping_route_cache WHERE origin_facility_id = v_final_factory AND destination_country = v_country;
        v_ship := CASE WHEN v_fallback = 'RoutesUnavailable' THEN NULL ELSE v_route.cost_amount END;
        v_alloc_id := md5('demo-allocation-' || i)::uuid;
        v_page := v_final_factory || '-' || ((v_pages_base ->> v_final_factory)::int + i % 3);

        INSERT INTO balancer.allocation(id, order_id, order_item_id, sku_uuid, quantity, fulfillment_type, destination_country,
            promised_delivery_date, sla_delivery_date, is_event_order, event_name, site, paid_at, cbd, mode, status, fallback_reason,
            quote_key, selected_quote_id, factory_id, original_factory_id, packing_facility_id, production_page,
            material_cost, labor_cost, packing_cost, production_cost, shipping_cost, total_cost, currency, cheapest_feasible_total_cost,
            actual_labor_cost, eta_from, eta_to, due_date, meets_promise, is_split, decision_reason, candidates, settings_version,
            transfer_count, decided_at, output_published_at, produced_at)
        VALUES (v_alloc_id, v_order, v_item, v_sku::text, 1, 'Factory', v_country,
            v_promised, v_sla, v_event, CASE WHEN v_event THEN 'Black Friday' END, v_sites[1 + i % 4], v_decided - interval '2 minutes',
            (SELECT cbd FROM catalog.quote_set WHERE id = v_set_id), v_mode, v_status, v_fallback,
            v_final_key, v_final_quote.quote_id, v_final_factory, v_winner, v_final_factory,
            CASE WHEN v_mode <> 'Shadow' THEN v_page END,
            v_final_quote.material_cost, v_final_quote.labor_cost, v_final_quote.packing_cost, v_final_quote.total_cost,
            v_ship, v_final_quote.total_cost + coalesce(v_ship, 0), 'USD',
            CASE WHEN v_fallback IS NULL THEN v_cheapest END,
            NULL,
            CASE WHEN v_ship IS NOT NULL THEN v_decided_date + 3 + v_route.transit_days_min END,
            CASE WHEN v_ship IS NOT NULL THEN v_decided_date + 3 + v_route.transit_days_max END,
            v_due,
            CASE WHEN v_ship IS NOT NULL THEN v_decided_date + 3 + v_route.transit_days_max <= v_promised END,
            false, v_reason, v_cands, 1,
            CASE WHEN v_to IS NULL THEN 0 ELSE 1 END,
            v_decided, CASE WHEN v_mode <> 'Shadow' THEN v_decided + interval '3 seconds' END, v_produced);

        k := 0;
        FOR v_line IN SELECT * FROM jsonb_array_elements(v_final_quote.components) LOOP
            INSERT INTO balancer.allocation_cost_line(allocation_id, kind, code, quantity, quoted_unit_cost, quoted_cost, actual_quantity, actual_cost)
            VALUES (v_alloc_id, 'Component', coalesce(v_line ->> 'alternativeUsed', v_line ->> 'erpId'), (v_line ->> 'quantity')::numeric,
                (v_line ->> 'unitCost')::numeric, coalesce((v_line ->> 'cost')::numeric, 0),
                CASE WHEN v_status = 'Produced' THEN (v_line ->> 'quantity')::numeric END,
                CASE WHEN v_status = 'Produced' THEN coalesce((v_line ->> 'cost')::numeric, 0) END);
        END LOOP;
        v_actual_labor := 0;
        FOR v_line IN SELECT * FROM jsonb_array_elements(v_final_quote.processes) LOOP
            k := k + 1;
            INSERT INTO balancer.allocation_cost_line(allocation_id, kind, code, quantity, quoted_unit_cost, quoted_cost, actual_quantity, actual_cost)
            VALUES (v_alloc_id, CASE WHEN v_line ->> 'processId' = 'PACKING' THEN 'Packing' ELSE 'Process' END, v_line ->> 'processId',
                (v_line ->> 'quantity')::numeric,
                round((v_line ->> 'laborCost')::numeric / (v_line ->> 'quantity')::numeric, 4),
                (v_line ->> 'laborCost')::numeric,
                CASE WHEN v_status = 'Produced' THEN round((v_line ->> 'quantity')::numeric * (0.9 + ((i + k) % 5) * 0.05), 4) END,
                CASE WHEN v_status = 'Produced' THEN round((v_line ->> 'laborCost')::numeric * (0.9 + ((i + k) % 5) * 0.05), 4) END);
            v_actual_labor := v_actual_labor + round((v_line ->> 'laborCost')::numeric * (0.9 + ((i + k) % 5) * 0.05), 4);
        END LOOP;
        IF v_status = 'Produced' THEN
            UPDATE balancer.allocation SET actual_labor_cost = v_actual_labor WHERE id = v_alloc_id;
        END IF;

        IF v_to IS NOT NULL THEN
            SELECT * INTO v_old_quote FROM catalog.quote WHERE quote_id = v_key || '-' || v_winner;
            SELECT * INTO v_old_route FROM balancer.shipping_route_cache WHERE origin_facility_id = v_winner AND destination_country = v_country;
            INSERT INTO balancer.allocation_transfer(allocation_id, order_id, order_item_id, sku_uuid, from_factory_id, from_page, to_factory_id,
                to_page, reason, note, old_quote_id, new_quote_id, cost_delta, currency, old_eta, new_eta, transferred_by, initiator, transferred_at)
            VALUES (v_alloc_id, v_order, v_item, v_sku::text, v_winner,
                v_winner || '-' || ((v_pages_base ->> v_winner)::int + i % 3), v_to, v_page,
                CASE i WHEN 28 THEN 'OverCapacity' WHEN 44 THEN 'ProcessOffline' ELSE 'ComponentOos' END,
                CASE i WHEN 28 THEN 'Polishing over capacity today' WHEN 44 THEN 'Machine down until next week, moved to keep the promise date'
                    ELSE 'Component out of stock; no balancer alternative in stock' END,
                v_old_quote.quote_id, v_final_quote.quote_id,
                round((v_final_quote.total_cost + v_route.cost_amount) - (v_old_quote.total_cost + v_old_route.cost_amount), 4), 'USD',
                v_decided_date + 3 + v_old_route.transit_days_max, v_decided_date + 3 + v_route.transit_days_max,
                CASE WHEN i = 47 THEN 'Balancer' WHEN i = 28 THEN 'ravit.tal@tenengroup.com' ELSE v_managers ->> v_winner END,
                CASE WHEN i = 47 THEN 'Balancer' ELSE 'User' END,
                v_transferred_at);
        END IF;

        IF v_status = 'Produced' THEN
            INSERT INTO balancer.production_record(allocation_id, source_event_id, order_id, order_item_id, sku_uuid, quote_key, quote_id,
                factory_id, packing_facility_id, sent_at, production_days, is_reproduction, reproduction_of_quote_id,
                reproduction_of_record_id, production_log, created_at)
            VALUES (v_alloc_id, 'tgs-sent-' || v_order || '-' || v_item, v_order, v_item, v_sku::text, v_final_key, v_final_quote.quote_id,
                v_final_factory, v_final_factory, v_produced,
                greatest(1, ceil(extract(epoch FROM v_produced - (v_decided - interval '2 minutes')) / 86400))::int,
                false, NULL, NULL, pg_temp.demo_production_log(v_sku, v_final_factory), v_produced)
            RETURNING id INTO v_record_id;

            IF i = 20 THEN
                v_rep_produced := v_produced + interval '2 days';
                INSERT INTO catalog.quote(quote_id, quote_set_id, factory_id, producible, reasons, material_cost, labor_cost, packing_cost,
                    total_cost, currency, capacity_signal_pct, share_pct, components, processes, status, selected_at, produced_at,
                    reproduction_of_quote_id, production_log, created_at)
                SELECT quote_id || '-RP1', quote_set_id, factory_id, producible, reasons, material_cost, labor_cost, packing_cost,
                    total_cost, currency, capacity_signal_pct, share_pct, components, processes, 'Produced', v_rep_produced, v_rep_produced,
                    id, production_log, v_rep_produced
                FROM catalog.quote WHERE quote_id = v_final_quote.quote_id
                RETURNING id INTO v_rep_quote_id;

                INSERT INTO balancer.production_record(allocation_id, source_event_id, order_id, order_item_id, sku_uuid, quote_key, quote_id,
                    factory_id, packing_facility_id, sent_at, production_days, is_reproduction, reproduction_of_quote_id,
                    reproduction_of_record_id, production_log, created_at)
                VALUES (v_alloc_id, 'tgs-sent-' || v_order || '-' || v_item || '-rp1', v_order, v_item, v_sku::text, v_final_key,
                    v_final_quote.quote_id || '-RP1', v_final_factory, v_final_factory, v_rep_produced, 2, true, v_final_quote.quote_id,
                    v_record_id, pg_temp.demo_production_log(v_sku, v_final_factory), v_rep_produced);
            END IF;
        END IF;

        IF v_mode = 'Shadow' THEN
            INSERT INTO balancer.legacy_decision(order_id, order_item_id, factory_id, total_cost, eta, received_at)
            SELECT v_order, v_item, lq.factory_id, lq.total_cost + coalesce(r.cost_amount, 0), v_decided_date + 3 + r.transit_days_max,
                   v_decided - interval '1 minute'
            FROM catalog.quote lq
            LEFT JOIN balancer.shipping_route_cache r ON r.origin_facility_id = lq.factory_id AND r.destination_country = v_country
            WHERE lq.quote_set_id = v_set_id AND lq.producible
            ORDER BY CASE WHEN i % 3 = 0 THEN lq.factory_id <> v_winner ELSE lq.factory_id = v_winner END DESC, lq.factory_id
            LIMIT 1;
        END IF;
    END LOOP;

    INSERT INTO balancer.allocation(id, order_id, order_item_id, sku_uuid, quantity, fulfillment_type, destination_country,
        promised_delivery_date, is_event_order, site, paid_at, cbd, mode, status, total_cost, currency, is_split, decision_reason,
        candidates, settings_version, transfer_count, decided_at, output_published_at)
    SELECT md5('demo-allocation-ds-' || d.n)::uuid, d.order_id, 'ITM001', s.sku_uuid::text, 1, 'Dropshipper', d.country,
        (v_now - d.ago)::date + 10, false, d.site, v_now - d.ago - interval '2 minutes', '[]'::jsonb, 'Live', 'NotRouted',
        s.dropshipper_total_cost_amount, s.dropshipper_total_cost_currency, false, 'Dropshipper item: not routed to a TG factory',
        '[]'::jsonb, 1, 0, v_now - d.ago, v_now - d.ago + interval '3 seconds'
    FROM (VALUES
        ('22', 'ORD884351', 'US', 'Myka', interval '3 days 4 hours'),
        ('23', 'ORD884352', 'GB', 'OAL', interval '1 day 2 hours')
    ) AS d(n, order_id, country, site, ago)
    JOIN catalog.sku s ON s.sku_uuid = ('de300000-0000-4000-8000-0000000000' || d.n)::uuid;
END
$$;

COMMIT;

/* global React, COMPONENTS, PROCESSES, SKUS, FACTORIES */
/* Phase 1 data store — Components, Process capacity, SKUs, Balancer.
   Derived from the full-vision seed data; mutated in place, re-rendered via useP1Store(). */

const P1_F = ['KG', 'NZ', 'HU', 'TH'];
const P1_NOW = 'Nov 23, 2026 · 10:14';

const P1_ROLES = {
  factory_manager: { id: 'factory_manager', label: 'Factory manager', icon: 'factory', color: '#1976d2' },
  supply_chain:    { id: 'supply_chain', label: 'Supply chain manager', name: 'Maya Ben-David', initials: 'MB', icon: 'local_shipping', color: '#00838f' },
  catalog_admin:   { id: 'catalog_admin', label: 'Factory catalog admin', name: 'Dana Ofer', initials: 'DO', icon: 'admin_panel_settings', color: '#2e7d32' },
  floor_user:      { id: 'floor_user', label: 'Production floor user', name: 'Eli Mor', initials: 'EM', icon: 'visibility', color: '#607d8b', viewOnly: true },
  ops_manager:     { id: 'ops_manager', label: 'Operations manager', name: 'Ravit Tal', initials: 'RT', icon: 'hub', color: '#ed6c02' },
  finance:         { id: 'finance', label: 'Finance', name: 'Yoni Shapira', initials: 'YS', icon: 'payments', color: '#5d4037' },
};
const P1_FACTORY_MANAGERS = {
  KG: { name: 'Avi Levi', initials: 'AL' }, NZ: { name: 'Samir Haddad', initials: 'SH' },
  HU: { name: 'Zsófia Nagy', initials: 'ZN' }, TH: { name: 'Somchai Pong', initials: 'SP' },
};
function p1User(role, factory) {
  const r = P1_ROLES[role] || P1_ROLES.floor_user;
  if (role === 'factory_manager') { const m = P1_FACTORY_MANAGERS[factory] || P1_FACTORY_MANAGERS.NZ; return { ...r, name: m.name, initials: m.initials, label: `Factory manager · ${factory}` }; }
  return r;
}

/* Who may edit what */
const P1_CAN = {
  markOOS:        (c, f) => c.role === 'factory_manager' && c.factory === f,
  editAlternatives: (c) => c.role === 'supply_chain',
  editCritical:   (c) => c.role === 'supply_chain',
  editCapacity:   (c, f) => c.role === 'factory_manager' && c.factory === f,
  editShare:      (c, f) => c.role === 'factory_manager' && c.factory === f,
  editDefaultShare: (c) => c.role === 'catalog_admin',
  newProcess:     (c) => c.role === 'catalog_admin',
  editProcessFactories: (c) => c.role === 'catalog_admin',
  syncNow:        (c) => c.role === 'catalog_admin',
  seeAllFactories:(c) => c.role === 'ops_manager',
  transfer:       (c, f) => (c.role === 'factory_manager' && c.factory === f) || c.role === 'ops_manager',
};

const _p1h = (s) => { let h = 7; for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0; return Math.abs(h); };

/* ---------------- Components ---------------- */
const P1_COMP = {};
(typeof COMPONENTS !== 'undefined' ? COMPONENTS : []).forEach((c, i) => {
  const idx = i + 1;
  const stock = {};
  P1_F.forEach((f, fi) => {
    let total = c.stock[f];
    if ((idx + fi) % 11 === 3) total = 0;
    else if ((idx + fi * 2) % 7 === 2) total = Math.round(c.critical_threshold * (0.35 + (idx % 4) * 0.12));
    const wh = Math.round(total * (0.62 + ((idx + fi) % 4) * 0.06));
    stock[f] = { wh, floor: total - wh };
  });
  const override = {};
  if (idx % 5 === 1) override.HU = Math.round(c.critical_threshold * 1.4);
  if (idx % 8 === 4) override.TH = Math.round(c.critical_threshold * 0.6);
  const alts = (COMPONENTS.filter(x => x.family === c.family && x.id !== c.id).slice(0, idx % 3).map(x => x.id));
  P1_COMP[c.id] = {
    id: c.id, erp_id: c.erp_id, name: c.name, family: c.family, material: c.material, measure: c.measure,
    supplier: c.provider_id, unit_cost: c.cost_per_unit, currency: c.cost_currency || 'USD', uom: c.measure ? c.measure.unit : 'pcs',
    stock, critical_global: c.critical_threshold, critical_override: override, manual_oos: {}, alternatives: alts,
    history: [
      { who: 'SAP sync', when: 'Nov 23, 2026 · 06:00', field: 'Stock · NZ warehouse', old: (stock.NZ.wh + 60).toLocaleString(), nw: stock.NZ.wh.toLocaleString() },
      { who: 'SAP sync', when: 'Nov 22, 2026 · 06:00', field: 'Stock · KG floor', old: (stock.KG.floor + 25).toLocaleString(), nw: stock.KG.floor.toLocaleString() },
      { who: 'Maya Ben-David', when: 'Oct 30, 2026 · 14:12', field: 'Critical level (global)', old: Math.round(c.critical_threshold * 0.8).toLocaleString(), nw: c.critical_threshold.toLocaleString() },
    ],
  };
});
/* seeded manual OOS flags — one conflict, one real OOS, one conflict at TH */
function _seedOOS(id, f, note, by, at) { const c = P1_COMP[id]; if (c) c.manual_oos[f] = { on: true, note, by, at }; }
_seedOOS('COMP-0003', 'NZ', 'Batch failed QC — quarantined', 'Samir Haddad', 'Nov 18, 2026 · 08:40');
_seedOOS('COMP-0027', 'HU', 'Supplier delay', 'Zsófia Nagy', 'Nov 20, 2026 · 11:05');
_seedOOS('COMP-0012', 'TH', '', 'Somchai Pong', 'Nov 12, 2026 · 16:30');
_seedOOS('COMP-0009', 'NZ', 'Wrong size delivered', 'Samir Haddad', 'Nov 21, 2026 · 09:15');
['COMP-0001', 'COMP-0002', 'COMP-0003', 'COMP-0005'].forEach(id => _seedOOS(id, 'HU', 'Supply scheduled for Feb 1', 'Zsófia Nagy', 'Nov 19, 2026 · 13:20'));
if (P1_COMP['COMP-0027']) P1_COMP['COMP-0027'].stock.HU = { wh: 40, floor: 12 };

function p1CritFor(c, f) { return c.critical_override[f] != null ? c.critical_override[f] : c.critical_global; }
function p1StockStatus(c, f) {
  const s = c.stock[f]; const total = s.wh + s.floor; const crit = p1CritFor(c, f); const m = c.manual_oos[f] && c.manual_oos[f].on;
  if (m && total > crit) return 'OOS_CONFLICT';
  if (m || total === 0) return 'OOS';
  if (total < crit) return 'LOW';
  return 'OK';
}
const P1_RANK = { OK: 0, LOW: 1, OOS: 2, OOS_CONFLICT: 3 };
function p1WorstStatus(c, factories) {
  let worst = 'OK', at = [];
  (factories || P1_F).forEach(f => { const s = p1StockStatus(c, f); if (P1_RANK[s] > P1_RANK[worst]) { worst = s; at = [f]; } else if (s === worst && s !== 'OK') at.push(f); });
  return { status: worst, at };
}

/* ---------------- Processes ---------------- */
const P1_CURRENCY = { KG: 'ILS', NZ: 'ILS', HU: 'HUF', TH: 'THB' };
const P1_FX = { ILS: 0.27, HUF: 0.0027, THB: 0.028, USD: 1 };
const P1_MACHINE_MODEL = { KG: '', NZ: '', HU: ' · Gen 2', TH: ' · Asia model' };
const P1_PROC = {};
(typeof PROCESSES !== 'undefined' ? PROCESSES : []).forEach((p, i) => {
  const idx = i + 1;
  const factories = {};
  P1_F.forEach((f, fi) => {
    const runs = !((idx + fi * 3) % 9 === 4) && !(p.stage === 'plating' && f === 'TH');
    const h = (idx * 7 + fi * 13) % 17;
    const cap = !runs ? 0 : h === 3 ? 0 : h === 5 ? 40 : h === 9 ? 70 : h === 12 ? 90 : 100;
    const cur = P1_CURRENCY[f]; const usd = p.labor_cost[f];
    factories[f] = {
      runs, machine_type: p.process_kind === 'machine' ? ((p.machines_required[0] || 'Bench') + P1_MACHINE_MODEL[f]) : null,
      labor_cost: +(usd / P1_FX[cur]).toFixed(cur === 'HUF' ? 0 : 2), currency: cur, capacity: cap,
      reason: cap === 0 && runs ? 'Machine down — awaiting spare part' : cap > 0 && cap < 100 ? 'Reduced staffing this week' : '',
      recovery: cap < 100 && runs ? 'Nov 28, 2026' : '',
      changed_by: P1_FACTORY_MANAGERS[f].name, changed_at: cap < 100 ? 'Nov 21, 2026' : 'Oct 02, 2026',
      /* 100% = max order items per day for this process at this factory; used_today = items already done today */
      max_per_day: { KG: 1000, NZ: 600, HU: 400, TH: 200 }[f] - ((idx * 37) % 9) * 10,
      used_today: 0,
    };
    const fx = factories[f]; fx.used_today = Math.round(fx.max_per_day * cap / 100 * (((idx + fi * 3) % 6) / 10));
  });
  P1_PROC[p.id] = {
    id: p.id, name: p.name, type: p.process_kind === 'machine' ? 'Machine' : 'Work', description: p.description, status: 'Active', factories,
    uom: p.uom_unit || 'minutes',
    history: [
      { who: P1_FACTORY_MANAGERS.NZ.name, when: 'Nov 21, 2026 · 07:50', field: 'Capacity · NZ', old: '100%', nw: factories.NZ.capacity + '%' },
      { who: 'Dana Ofer', when: 'Sep 14, 2026 · 12:02', field: 'Labor cost · HU', old: Math.round(factories.HU.labor_cost * 0.92) + ' HUF', nw: factories.HU.labor_cost + ' HUF' },
    ],
  };
});
const P1_UOM_SHORT = { minutes: 'min', seconds: 'sec', hours: 'hr', units: 'units', pieces: 'pcs', cm: 'cm', mm: 'mm', 'cm²': 'cm²', 'mm²': 'mm²' };
const P1_UOMS = ['minutes', 'seconds', 'hours', 'units', 'pieces', 'cm', 'mm', 'cm²', 'mm²'];
function p1Qty(q, uom) { return `${q} ${P1_UOM_SHORT[uom] || uom}`; }
function p1LaborUSD(fx) { return fx.labor_cost * P1_FX[fx.currency]; }
/* Today: available = max × capacity %; remaining % is measured against the max (P-14) */
function p1Today(x) {
  const available = Math.round(x.max_per_day * x.capacity / 100);
  const used = Math.min(x.used_today || 0, available);
  return { max: x.max_per_day, available, used, left: available - used, remainingPct: x.max_per_day ? Math.max(0, Math.round((available - used) / x.max_per_day * 100)) : 0 };
}
/* Daily plan: items allocated to a factory for a day, not produced yet (synthetic sample data) */
const P1_PLAN_DAYS = ['Nov 24, 2026', 'Nov 25, 2026', 'Nov 26, 2026'];
function p1DailyPlan(f, dayIdx) {
  const skus = Object.values(P1_SKU).filter(s => s.type === 'Manufactured' && s.share[f] && s.share[f].producible);
  const rows = [];
  Object.values(P1_PROC).filter(p => p.factories[f].runs).forEach((p, i) => {
    const x = p.factories[f]; const t = p1Today(x);
    const using = skus.filter(s => s.processes.some(m => m.procId === p.id));
    if (!using.length) return;
    const ratio = [0.45, 0.7, 0.85, 1.05, 1.25, 0.6, 0.95][(i + dayIdx * 2) % 7];
    const base = dayIdx === 0 ? t.left : t.available;
    const planned = Math.max(1, Math.round(base * ratio));
    const unitQty = using[0].processes.find(m => m.procId === p.id).qty;
    const items = Array.from({ length: Math.min(planned, 8) }, (_, k) => { const s = using[(k + i) % using.length]; const n = 884200 + ((i * 37 + k * 11 + dayIdx * 101) % 700);
      return { orderId: `ORD-${n}`, itemId: `ITM00${1 + (k % 3)}`, sku: s.site_sku, skuName: s.name, quoteId: `PQ-ORD-${n}-ITM00${1 + (k % 3)}-${f}`, due: P1_PLAN_DAYS[Math.min(2, dayIdx + (k % 2))] }; });
    rows.push({ proc: p, planned, qty: +(planned * unitQty).toFixed(1), done: dayIdx === 0 ? t.used : 0, available: t.available, items });
  });
  return rows;
}
/* Transfers: open (not produced) items across suppliers, searchable like TG Supplier today
   (order, supplier, page, event, site, due day), and the transfer log */
const P1_TRANSFER_REASONS = ['Process offline', 'Component out of stock', 'Over capacity', 'Quality issue', 'Customer request', 'Other'];
const P1_SITES = ['Myka', 'OAL', 'SETT', 'L&L'];
const P1_EVENTS = ['Regular', 'Black Friday', 'Christmas', "Valentine's Day"];
function p1Pages(f) { const b = { KG: 2140, NZ: 3310, HU: 4025, TH: 5180 }[f] || 1000; return [0, 1, 2, 3].map(k => `${f}-${b + k}`); }
function p1TransferItems() {
  const seen = {}; const out = [];
  P1_F.forEach((f, fi) => [0, 1, 2].forEach(di => p1DailyPlan(f, di).forEach((r, ri) => r.items.forEach((it, k) => { const key = it.orderId + it.itemId; if (seen[key]) return; seen[key] = 1;
    const h = it.orderId.charCodeAt(8) + it.orderId.charCodeAt(9) + k + ri;
    out.push({ key, ...it, factory: f, page: p1Pages(f)[(di + h) % 4], event: P1_EVENTS[h % 4 === 3 ? 1 : h % 7 === 0 ? 2 : 0], site: P1_SITES[h % 4], day: P1_PLAN_DAYS[di], process: r.proc.name, cost: +(8 + (h % 9) * 0.7).toFixed(2) }); }))));
  const moved = P1_STORE.transferred || {};
  return out.map(it => moved[it.key] ? { ...it, factory: moved[it.key].to, page: moved[it.key].page, quoteId: it.quoteId.replace(/-[A-Z]{2}$/, '-' + moved[it.key].to) } : it);
}
const P1_TRANSFER_LOG = [
  { when: 'Nov 24, 2026 · 08:41', who: 'Samir Haddad', orderId: 'ORD-884311', itemId: 'ITM001', sku: '110-01-2568-21', from: 'NZ', fromPage: 'NZ-3311', to: 'HU', toPage: 'HU-4026', reason: 'Process offline', note: 'Laser engraving down until Nov 28', oldQuote: 'PQ-ORD-884311-ITM001-NZ', newQuote: 'PQ-ORD-884311-ITM001-HU', costDelta: 0.8, etaFrom: 'Nov 28', etaTo: 'Nov 29', by: 'Manual' },
  { when: 'Nov 24, 2026 · 08:41', who: 'Samir Haddad', orderId: 'ORD-884311', itemId: 'ITM002', sku: '110-01-2554-08', from: 'NZ', fromPage: 'NZ-3311', to: 'HU', toPage: 'HU-4026', reason: 'Process offline', note: 'Same order — kept together', oldQuote: 'PQ-ORD-884311-ITM002-NZ', newQuote: 'PQ-ORD-884311-ITM002-HU', costDelta: 0.5, etaFrom: 'Nov 28', etaTo: 'Nov 29', by: 'Manual' },
  { when: 'Nov 23, 2026 · 17:05', who: 'Balancer', orderId: 'ORD-884260', itemId: 'ITM001', sku: '110-07-3322-19', from: 'TH', fromPage: 'TH-5181', to: 'KG', toPage: 'KG-2141', reason: 'Component out of stock', note: 'Lobster Clasp 10mm OOS at TH; no For balancer alternative in stock', oldQuote: 'PQ-ORD-884260-ITM001-TH', newQuote: 'PQ-ORD-884260-ITM001-KG', costDelta: 1.4, etaFrom: 'Dec 01', etaTo: 'Nov 30', by: 'Balancer' },
  { when: 'Nov 23, 2026 · 11:20', who: 'Ravit Tal', orderId: 'ORD-884198', itemId: 'ITM003', sku: '110-04-2901-12', from: 'HU', fromPage: 'HU-4025', to: 'NZ', toPage: 'NZ-3312', reason: 'Over capacity', note: 'Plating layer 1 at HU over by 140 today', oldQuote: 'PQ-ORD-884198-ITM003-HU', newQuote: 'PQ-ORD-884198-ITM003-NZ', costDelta: -0.3, etaFrom: 'Nov 30', etaTo: 'Nov 29', by: 'Manual' },
  { when: 'Nov 22, 2026 · 15:52', who: 'Zsófia Nagy', orderId: 'ORD-884102', itemId: 'ITM001', sku: '110-02-6691-71', from: 'HU', fromPage: 'HU-4027', to: 'TH', toPage: 'TH-5180', reason: 'Quality issue', note: 'Stone batch rejected by QA', oldQuote: 'PQ-ORD-884102-ITM001-HU', newQuote: 'PQ-ORD-884102-ITM001-TH', costDelta: 0.9, etaFrom: 'Nov 27', etaTo: 'Nov 29', by: 'Manual' },
];
function p1CapStatus(v) { return v === 0 ? 'Offline' : v === 100 ? 'Full' : 'Reduced'; }

/* ---------------- SKUs ---------------- */
const P1_DEFAULT_SHARE = { KG: 10, NZ: 35, HU: 30, TH: 25 };
const P1_DROPSHIPPERS = ['Lumière Supply Co.', 'Goldline Direct', 'Sunrise Jewelry Ltd.'];
const P1_DROPSHIP_IDS = ['110-12-4492-33', '110-09-7745-02'];
const P1_SKU = {};
const P1_SKU_ORDER = [];
(typeof SKUS !== 'undefined' ? SKUS : []).forEach((s, i) => {
  const live = s.versions.find(v => v.live) || s.versions[0];
  const d = (live && live.data) || {};
  const h = _p1h(s.product_id);
  const drop = P1_DROPSHIP_IDS.includes(s.product_id) || (i >= 14 && i % 9 === 5);
  const compIds = Object.keys(P1_COMP);
  const comps = drop ? [] : Array.from({ length: 3 + (h % 4) }, (_, k) => compIds[(h + k * 7) % compIds.length])
    .filter((v, k, a) => a.indexOf(v) === k)
    .map((id, k) => ({ compId: id, qty: k === 0 ? 1 : 1 + ((h + k) % 3), mandatory: k < 3 || (h + k) % 2 === 0 }));
  const procIds = Object.keys(P1_PROC);
  const procs = drop ? [] : [0, 2, 4, 8, 16, 20, 21].filter((_, k) => (h >> k) % 3 !== 0 || k === 6).map((n, k) => { const pid = procIds[(n + (h % 2)) % procIds.length]; const u = (P1_PROC[pid] && P1_PROC[pid].uom) || 'minutes'; const r = (h >> (k + 2)) % 8;
    const qty = u === 'units' ? 1 + (r % 3) : u === 'cm' ? 2 + r : u === 'cm²' ? +(1.5 + r * 0.5).toFixed(1) : +(0.5 + r * 0.75).toFixed(2);
    return { procId: pid, step: (k + 1) * 10, qty }; });
  const share = {};
  P1_F.forEach((f, fi) => { share[f] = { producible: !drop && ((h >> fi) % 4 !== 0 || f === 'NZ'), override: null }; });
  if (!drop && h % 5 === 1) { share.HU.override = 50; share.HU.producible = true; }
  if (!drop && h % 7 === 2) { share.TH.override = 15; share.TH.producible = true; }
  const issues = [];
  if (h % 9 === 4 && comps.length) { issues.push('Component ERP-10999 not found in SAP'); comps.push({ compId: null, missingErp: 'ERP-10999', qty: 1, mandatory: true }); }
  if (!drop && !P1_F.some(f => share[f].producible)) issues.push('No producible factory');
  if (h % 11 === 6 && !drop) issues.push('Process “Plating layer 2” has no running factory among producible factories');
  /* Production conflicts: processes used on the floor (from the TG Supplier production log) that aren't in the SKU's mapping */
  const conflicts = [];
  if (!drop && (h % 6 === 3 || s.product_id === '110-01-2568-21')) {
    const unlisted = procIds.filter(pid => !procs.some(p => p.procId === pid));
    const prodF = P1_F.filter(f => share[f].producible);
    const days = ['Nov 18, 2026', 'Nov 20, 2026', 'Nov 22, 2026', 'Nov 23, 2026'];
    [0, 1].forEach(k => {
      const pid = unlisted[(h + k * 5) % unlisted.length]; const f = prodF[(h + k) % prodF.length] || 'NZ';
      const pf = P1_PROC[pid] && P1_PROC[pid].factories[f];
      conflicts.push({ procId: pid, machine: (pf && pf.machine_type) || '—', orderId: `ORD-${884000 + ((h >> k) % 900)}`, factory: f, day: days[(h + k) % days.length] });
    });
  }
  const legacy = Object.values(s.site_ids || {}).filter(Boolean).slice(0, 2);
  P1_SKU[s.product_id] = {
    id: s.product_id, uuid: `a${(h % 0xfffffff).toString(16).padStart(7, '0')}-${(h % 0xffff).toString(16).padStart(4, '0')}-4${(h % 0xfff).toString(16).padStart(3, '0')}-9b1e-${s.product_id.replace(/-/g, '')}`,
    name: d.product_name || s.product_id, item_type: s.item_type, thumb: s.thumb_color,
    type: drop ? 'Dropshipper' : 'Manufactured',
    status: ['offline', 'archived'].includes(s.item_status) ? 'Retired' : 'Active',
    site_sku: s.product_id, factory_sku: s.factory_sku_new || d.tgi_id || '—', legacy,
    issues, conflicts, components: comps, processes: procs, share,
    dropshipper: drop ? { name: P1_DROPSHIPPERS[h % 3], total_cost: +(14 + (h % 40) + 0.5).toFixed(2) } : null,
    history: [
      { who: 'Master catalog sync', when: 'Nov 23, 2026 · 09:42', field: 'Status', old: 'Active', nw: 'Active' },
      { who: 'Master catalog sync', when: 'Nov 10, 2026 · 09:40', field: 'Component mapping', old: '—', nw: `${comps.length} components` },
    ],
  };
  P1_SKU_ORDER.push(s.product_id);
});

function p1EffectiveShare(sku, defaults) {
  const D = defaults || P1_STORE.defaultShare;
  const out = { KG: 0, NZ: 0, HU: 0, TH: 0 };
  const prod = P1_F.filter(f => sku.share[f].producible);
  const ov = prod.filter(f => sku.share[f].override != null);
  const rest = prod.filter(f => sku.share[f].override == null);
  const sumOv = ov.reduce((n, f) => n + sku.share[f].override, 0);
  ov.forEach(f => { out[f] = sku.share[f].override; });
  const dsum = rest.reduce((n, f) => n + D[f], 0);
  const pool = Math.max(0, 100 - sumOv);
  if (rest.length) {
    let acc = 0;
    rest.forEach((f, k) => {
      const v = k === rest.length - 1 ? pool - acc : Math.round(dsum ? pool * D[f] / dsum : pool / rest.length);
      out[f] = v; acc += v;
    });
  }
  return out;
}
function p1SkusUsingComp(compId) { return Object.values(P1_SKU).filter(s => s.components.some(c => c.compId && c.compId === compId)); }
function p1SkusUsingProc(procId) { return Object.values(P1_SKU).filter(s => s.processes.some(p => p.procId === procId)); }

/* ---------------- Store ---------------- */
const P1_STORE = {
  comps: P1_COMP, procs: P1_PROC, skus: P1_SKU, skuOrder: P1_SKU_ORDER,
  defaultShare: { ...P1_DEFAULT_SHARE },
  sync: {
    sap: { label: 'SAP', cadence: 'Daily · 06:00', last: 'Nov 23, 2026 · 06:00', result: 'Success', detail: '50 components · 312 stock rows updated' },
    mc:  { label: 'Master catalog', cadence: 'On event, plus daily · 09:40', last: 'Nov 23, 2026 · 09:42', result: 'Success', detail: `${P1_SKU_ORDER.length} SKUs · 2 validation issues` },
  },
  audit: [],
  transferred: {},
  transferLog: P1_TRANSFER_LOG,
};
const _p1Listeners = new Set();
function p1Bump() { _p1Listeners.forEach(fn => fn()); }
function useP1Store() {
  const [, force] = React.useState(0);
  React.useEffect(() => { const l = () => force(x => x + 1); _p1Listeners.add(l); return () => _p1Listeners.delete(l); }, []);
  return P1_STORE;
}
function p1Log(entity, ref, who, field, old, nw) {
  P1_STORE.audit.unshift({ entity, ref, who, when: P1_NOW, field, old, nw });
}

/* ---------------- Routing / balancer ---------------- */
const P1_ROUTING = {
  tiles: { routed: 18452, transfer_rate: 6.8, on_time: 93.4, cost_vs_cheapest: 3.1 },
  shares: { KG: { target: 10, actual: 8.6 }, NZ: { target: 35, actual: 37.9 }, HU: { target: 30, actual: 28.2 }, TH: { target: 25, actual: 25.3 } },
  shadow: { agreement: 87.2, savings: 41800, compared: 6240, disagree_top: [
    { reason: 'New balancer chose cheaper factory (component OOS at legacy pick)', share: 46 },
    { reason: 'Legacy ignored process capacity reduction', share: 31 },
    { reason: 'Shipping route cost difference', share: 23 },
  ] },
  countries: ['All countries', 'United States', 'United Kingdom', 'Germany', 'Israel', 'Australia', 'Canada'],
};

function _cand(f, prod, ship, eta, excluded) { return { factory: f, prod, ship, total: excluded ? null : +(prod + ship).toFixed(2), eta, excluded }; }
const P1_DECISIONS = [
  { order: 'ORD-884120', placed: 'Nov 23, 2026 · 08:12', dest: 'United States', items: [
    { sku: '110-01-2568-21', winner: 'NZ', packing: 'NZ', mode: 'Live', cands: [
      _cand('KG', 11.40, 6.20, 'Nov 30', 'Not producible for this SKU'), _cand('NZ', 9.80, 5.90, 'Nov 28'),
      _cand('HU', 10.60, 7.40, 'Dec 01'), _cand('TH', 8.90, 9.10, 'Dec 03', 'Component OOS — Lobster Clasp 10mm') ] },
    { sku: '110-01-2554-08', winner: 'HU', packing: 'NZ', mode: 'Live', cands: [
      _cand('KG', 10.10, 6.20, 'Nov 30', 'Not producible for this SKU'), _cand('NZ', 9.40, 5.90, 'Nov 28', 'Process offline — Laser engraving (0%)'),
      _cand('HU', 9.90, 7.10, 'Nov 29'), _cand('TH', 8.70, 9.10, 'Dec 03') ] } ] },
  { order: 'ORD-884133', placed: 'Nov 23, 2026 · 08:19', dest: 'Germany', items: [
    { sku: '110-07-3322-19', winner: 'HU', packing: 'HU', mode: 'Shadow', legacy: 'NZ', cands: [
      _cand('KG', 12.30, 7.80, 'Dec 02'), _cand('NZ', 10.90, 7.20, 'Dec 01'),
      _cand('HU', 11.20, 3.40, 'Nov 27'), _cand('TH', 9.60, 10.40, 'Dec 05', 'No route — TH → DE lane suspended') ] } ] },
  { order: 'ORD-884140', placed: 'Nov 23, 2026 · 08:26', dest: 'United Kingdom', items: [
    { sku: '110-02-6691-71', winner: 'NZ', packing: 'NZ', mode: 'Fallback', cands: [
      _cand('KG', null, null, '—', 'Not producible for this SKU'), _cand('NZ', 7.40, 6.10, 'Nov 29'),
      _cand('HU', null, null, '—', 'Cost data missing — fallback to default share'), _cand('TH', null, null, '—', 'Not producible for this SKU') ] } ] },
  { order: 'ORD-884152', placed: 'Nov 23, 2026 · 08:41', dest: 'Australia', items: [
    { sku: '110-04-2901-12', winner: 'TH', packing: 'TH', mode: 'Live', cands: [
      _cand('KG', 9.10, 11.20, 'Dec 04'), _cand('NZ', 8.80, 10.90, 'Dec 03'),
      _cand('HU', 9.30, 12.40, 'Dec 06', 'Process at 0% — Plating layer 1'), _cand('TH', 8.20, 6.30, 'Nov 30') ] } ] },
  { order: 'ORD-884167', placed: 'Nov 23, 2026 · 09:03', dest: 'Israel', items: [
    { sku: '110-01-2568-22', winner: 'KG', packing: 'KG', mode: 'Live', cands: [
      _cand('KG', 10.20, 1.40, 'Nov 25'), _cand('NZ', 9.70, 1.60, 'Nov 25', 'Component OOS — Cable Chain 1.2mm — 18in'),
      _cand('HU', 10.40, 6.80, 'Nov 30'), _cand('TH', 8.90, 8.20, 'Dec 02') ] } ] },
];

/* CSV export ("Export to Excel") */
function p1ExportCsv(filename, headers, rows) {
  const esc = v => { const s = v == null ? '' : String(v); return /[",\n]/.test(s) ? '"' + s.replace(/"/g, '""') + '"' : s; };
  const csv = '\ufeff' + [headers, ...rows].map(r => r.map(esc).join(',')).join('\n');
  const a = document.createElement('a');
  a.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
  a.download = filename; document.body.appendChild(a); a.click(); a.remove();
}

Object.assign(window, {
  P1_F, P1_NOW, P1_ROLES, P1_FACTORY_MANAGERS, p1User, P1_CAN, P1_STORE, useP1Store, p1Bump, p1Log,
  p1CritFor, p1StockStatus, p1WorstStatus, P1_RANK, P1_FX, p1LaborUSD, p1CapStatus,
  p1EffectiveShare, p1SkusUsingComp, p1Today, p1DailyPlan, P1_PLAN_DAYS, p1TransferItems, P1_TRANSFER_REASONS, P1_SITES, P1_EVENTS, p1Pages, P1_UOMS, P1_UOM_SHORT, p1Qty, p1SkusUsingProc, P1_ROUTING, P1_DECISIONS, p1ExportCsv,
});

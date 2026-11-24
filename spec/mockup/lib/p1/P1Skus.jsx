/* global React, Icon, IconButton, Button, Chip, Switch, P1_F, P1_STORE, P1_CAN, P1_NOW, useP1Store, useP1Ctx, p1Bump, p1Log,
   p1StockStatus, p1EffectiveShare, p1th, p1td, p1mono, p1card, p1select, p1input, SourceBadge, StockChip, CapChip, FactoryTag,
   ReadOnlyNote, P1Tile, P1Tabs, P1Back, P1Search, SyncStamp, HistoryTable, P1Field, P1Card, TypeChip, capColor */
const { useState: useStateP1S } = React;

function SkuTypeChip({ t }) {
  const d = t === 'Dropshipper';
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, height: 22, padding: '0 9px', borderRadius: 11, fontSize: 12, fontWeight: 500, background: d ? 'rgba(123,31,162,0.09)' : 'rgba(0,0,0,0.06)', color: d ? '#6a1b9a' : 'rgba(0,0,0,0.75)' }}><Icon name={d ? 'local_shipping' : 'factory'} size={14}/>{t}</span>;
}
function ValidChip({ ok }) {
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 12, fontWeight: 500, color: ok ? '#2e7d32' : '#c62828' }}><Icon name={ok ? 'check_circle' : 'error'} size={15}/>{ok ? 'Valid' : 'Invalid'}</span>;
}
function ConflictChip() {
  return <span title="The production floor used processes or machines that aren't in this SKU's mapping" style={{ display: 'inline-flex', alignItems: 'center', gap: 4, height: 22, padding: '0 9px', borderRadius: 11, fontSize: 12, fontWeight: 500, background: 'rgba(237,108,2,0.12)', color: '#b45309', whiteSpace: 'nowrap' }}><Icon name="report" size={14}/>Production conflict</span>;
}
function SkuStatus({ s }) { return <Chip label={s} color={s === 'Active' ? 'success' : 'default'} size="small"/>; }

function P1ProductsList({ onOpen }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const [q, setQ] = useStateP1S('');
  const [syncing, setSyncing] = useStateP1S(false);
  const all = S.skuOrder.map(id => S.skus[id]);
  const base = all.filter(s => { if (!q) return true; const x = q.toLowerCase(); return [s.uuid, s.site_sku, s.factory_sku, s.name].some(v => (v || '').toLowerCase().includes(x)); });
  const cf = useColFilters(base, {
    name: { type: 'text', get: s => s.name }, type: { type: 'enum', get: s => s.type, options: ['Manufactured', 'Dropshipper'] },
    site: { type: 'text', get: s => s.site_sku }, fsku: { type: 'text', get: s => s.factory_sku },
    status: { type: 'enum', get: s => s.status, options: ['Active', 'Retired'] },
    prod: { type: 'enum', get: s => P1_F.filter(f => s.share[f].producible), options: P1_F },
    val: { type: 'enum', get: s => s.issues.length ? 'Invalid' : 'Valid', options: ['Valid', 'Invalid'] },
  });
  const rows = cf.rows;
  const sync = () => { setSyncing(true); setTimeout(() => { S.sync.mc.last = P1_NOW; S.sync.mc.result = 'Success'; setSyncing(false); p1Bump(); ctx.toast('Master catalog sync complete'); }, 900); };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <SyncStamp label="Last master catalog sync" when={S.sync.mc.last}/><SourceBadge src="mc"/>
        <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.5)' }}>SKUs are read only — edit them in the master catalog.</span>
        <div style={{ flex: 1 }}></div>
        {P1_CAN.syncNow(ctx) && <Button variant="outlined" startIcon={syncing ? 'hourglass_top' : 'sync'} disabled={syncing} onClick={sync}>{syncing ? 'Syncing…' : 'Sync now'}</Button>}
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 12 }}>
        <P1Tile icon="inventory_2" label="Total SKUs" value={all.length}/>
        <P1Tile icon="local_shipping" label="Dropshipper" value={all.filter(s => s.type === 'Dropshipper').length} color="#6a1b9a"/>
        <P1Tile icon="error" label="Invalid" value={all.filter(s => s.issues.length).length} sub="need a fix in source" color="#d32f2f"/>
      </div>
      <div style={p1card}>
        <div style={{ padding: '12px 16px', display: 'flex', gap: 10, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
          <P1Search value={q} onChange={setQ} placeholder="Search UUID, Site SKU, Factory SKU or name…" width={420}/>
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead><tr><FilterTh k="site" label="Site SKU" cf={cf}/><FilterTh k="fsku" label="Factory SKU" cf={cf}/><FilterTh k="name" label="SKU name" cf={cf}/><FilterTh k="type" label="Type" cf={cf}/><FilterTh k="status" label="Status" cf={cf}/><FilterTh k="prod" label="Producible at" cf={cf}/><FilterTh k="val" label="Validation" cf={cf}/></tr></thead>
            <tbody>{rows.map(s => { const prod = P1_F.filter(f => s.share[f].producible); return (
              <tr key={s.id} onClick={() => onOpen(s.id)} style={{ cursor: 'pointer' }} onMouseEnter={e => e.currentTarget.style.background = 'rgba(0,0,0,0.03)'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                <td style={{ ...p1td, ...p1mono, color: '#1976d2', fontWeight: 500, whiteSpace: 'nowrap' }}>{s.site_sku}</td>
                <td style={{ ...p1td, ...p1mono, color: 'rgba(0,0,0,0.7)', whiteSpace: 'nowrap' }}>{s.factory_sku}</td>
                <td style={p1td}><div style={{ display: 'flex', alignItems: 'center', gap: 10 }}><span style={{ width: 28, height: 28, borderRadius: 6, background: s.thumb || '#eee', flexShrink: 0 }}></span><span style={{ fontWeight: 500 }}>{s.name}</span></div></td>
                <td style={p1td}><SkuTypeChip t={s.type}/></td>
                <td style={p1td}><SkuStatus s={s.status}/></td>
                <td style={p1td}>{s.type === 'Dropshipper' ? <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.45)' }}>Dropshipper</span> : prod.length ? <div style={{ display: 'flex', gap: 4 }}>{prod.map(f => <FactoryTag key={f} f={f}/>)}</div> : <span style={{ fontSize: 12, color: '#c62828' }}>None</span>}</td>
                <td style={p1td}><div style={{ display: 'flex', flexDirection: 'column', gap: 4, alignItems: 'flex-start' }}><ValidChip ok={!s.issues.length}/>{s.conflicts.length > 0 && <ConflictChip/>}</div></td>
              </tr>); })}
              {!rows.length && <tr><td colSpan="7" style={{ ...p1td, textAlign: 'center', padding: 28, color: 'rgba(0,0,0,0.5)' }}>No SKUs match.</td></tr>}
            </tbody>
          </table>
        </div>
        <FilterSummary cf={cf} total={all.length} noun="SKUs"/>
      </div>
    </div>
  );
}

/* ================= SKU detail ================= */
function P1SkuDetail({ id, onBack, onOpenComp, onOpenProc }) {
  const S = useP1Store();
  const s = S.skus[id]; const [tab, setTab] = useStateP1S('overview');
  if (!s) return null;
  const drop = s.type === 'Dropshipper';
  const tabs = drop ? [{ id: 'overview', label: 'Overview' }] : [
    { id: 'overview', label: 'Overview' }, { id: 'comps', label: 'Components', count: s.components.length },
    { id: 'procs', label: 'Processes', count: s.processes.length }, { id: 'fac', label: 'Factories' }, { id: 'history', label: 'Audit' }];
  const t = drop ? 'overview' : tab;
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <P1Back label="SKU" onClick={onBack} crumb={s.site_sku}/>
      <div style={{ ...p1card, padding: 22, display: 'flex', gap: 18, alignItems: 'center', flexWrap: 'wrap' }}>
        <span style={{ width: 60, height: 60, borderRadius: 10, background: s.thumb || '#eee' }}></span>
        <div style={{ flex: 1, minWidth: 260 }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 4, flexWrap: 'wrap' }}><SkuTypeChip t={s.type}/><SkuStatus s={s.status}/><ValidChip ok={!s.issues.length}/>{s.conflicts.length > 0 && <ConflictChip/>}<SourceBadge src="mc"/></div>
          <h2 style={{ margin: 0, fontSize: 22, fontWeight: 400 }}>{s.name}</h2>
          <div style={{ ...p1mono, color: 'rgba(0,0,0,0.6)', marginTop: 4 }}>{s.site_sku} · {s.factory_sku}</div>
        </div>
      </div>
      <P1Tabs value={t} onChange={setTab} tabs={tabs}/>
      {t === 'overview' && <P1SkuOverview s={s}/>}
      {t === 'comps' && <P1SkuComponents s={s} onOpenComp={onOpenComp}/>}
      {t === 'procs' && <P1SkuProcesses s={s} onOpenProc={onOpenProc}/>}
      {t === 'fac' && <P1SkuFactories s={s}/>}
      {t === 'history' && <HistoryTable rows={s.history}/>}
    </div>
  );
}

function P1SkuOverview({ s }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ padding: '14px 18px', borderRadius: 8, background: s.issues.length ? '#fdeded' : '#edf7ed', border: `1px solid ${s.issues.length ? 'rgba(211,47,47,0.3)' : 'rgba(46,125,50,0.3)'}`, display: 'flex', gap: 12 }}>
        <Icon name={s.issues.length ? 'error' : 'check_circle'} size={22} sx={{ color: s.issues.length ? '#d32f2f' : '#2e7d32', flexShrink: 0 }}/>
        <div style={{ fontSize: 14, color: s.issues.length ? '#5f2120' : '#1e4620' }}>
          <b style={{ fontWeight: 500 }}>{s.issues.length ? `Invalid — ${s.issues.length} issue${s.issues.length > 1 ? 's' : ''}` : 'Valid — ready for routing'}</b>
          {s.issues.length > 0 && <ul style={{ margin: '6px 0 0', paddingLeft: 18 }}>{s.issues.map(i => <li key={i}>{i}</li>)}</ul>}
          {s.issues.length > 0 && <div style={{ fontSize: 12, marginTop: 6, opacity: 0.8 }}>Fix the source data in the master catalog or SAP; the next sync re-validates.</div>}
        </div>
      </div>
      <P1Card title="Overview" right={<ReadOnlyNote text="Synced from the master catalog"/>}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', gap: 12 }}>
          <P1Field label="Name" value={s.name} badge="mc" wide/>
          <P1Field label="UUID" value={s.uuid} mono badge="mc" wide/>
          <P1Field label="Site SKU" value={s.site_sku} mono badge="mc"/>
          <P1Field label="Factory SKU" value={s.factory_sku} mono badge="mc"/>
          <P1Field label="Type" value={s.type} badge="mc"/>
          <P1Field label="Status" value={s.status} badge="mc"/>
          {s.dropshipper && <P1Field label="Dropshipper" value={s.dropshipper.name} badge="mc"/>}
          {s.dropshipper && <P1Field label="Total cost" value={`$${s.dropshipper.total_cost.toFixed(2)}`} mono badge="mc"/>}
          <div style={{ gridColumn: '1 / -1', padding: '11px 14px', borderRadius: 6, background: '#fafafa', border: '1px solid rgba(0,0,0,0.06)' }}>
            <div style={{ fontSize: 11, color: 'rgba(0,0,0,0.6)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 8 }}>Legacy IDs</div>
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              {s.legacy.length ? s.legacy.map(l => <span key={l} style={{ display: 'inline-flex', alignItems: 'center', gap: 8, padding: '4px 10px', borderRadius: 5, background: '#fff', border: '1px solid rgba(0,0,0,0.1)' }}><span style={{ ...p1mono, color: 'rgba(0,0,0,0.45)', textDecoration: 'line-through' }}>{l}</span><span style={{ fontSize: 10, padding: '1px 6px', borderRadius: 3, background: 'rgba(0,0,0,0.07)', color: 'rgba(0,0,0,0.55)', textTransform: 'uppercase', letterSpacing: '0.04em' }}>Deprecated alias</span></span>)
                : <span style={{ fontSize: 13, color: 'rgba(0,0,0,0.4)' }}>None</span>}
            </div>
          </div>
        </div>
      </P1Card>
    </div>
  );
}

function P1SkuComponents({ s, onOpenComp }) {
  const S = useP1Store();
  const cf = useColFilters(s.components, {
    erp: { type: 'text', get: m => m.compId ? S.comps[m.compId].erp_id : m.missingErp }, name: { type: 'text', get: m => m.compId ? S.comps[m.compId].name : 'Not found in SAP' },
    qty: { type: 'number', get: m => m.qty }, usage: { type: 'enum', get: m => m.mandatory ? 'Static' : 'Dynamic', options: ['Static', 'Dynamic'] },
    ...Object.fromEntries(P1_F.map(f => [f, { type: 'enum', get: m => m.compId ? ({ OK: 'OK', LOW: 'Low', OOS: 'OOS', OOS_CONFLICT: 'OOS conflict' })[p1StockStatus(S.comps[m.compId], f)] : '—', options: ['OK', 'Low', 'OOS', 'OOS conflict'] }])),
  });
  return (
    <P1Card title="Mapped components" right={<ReadOnlyNote text="Read only · Source: Master catalog"/>} pad={0}>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><FilterTh k="erp" label="ERP ID" cf={cf}/><FilterTh k="name" label="Component" cf={cf}/><FilterTh k="qty" label="Qty" cf={cf}/><FilterTh k="usage" label="Usage" cf={cf}/>{P1_F.map(f => <FilterTh key={f} k={f} label={f} cf={cf} align="center"/>)}</tr></thead>
          <tbody>{cf.rows.map((m, i) => { const c = m.compId && S.comps[m.compId]; return (
            <tr key={i} onClick={() => c && onOpenComp(c.id)} style={{ cursor: c ? 'pointer' : 'default', background: c ? 'transparent' : 'rgba(211,47,47,0.04)' }}>
              <td style={{ ...p1td, ...p1mono, color: c ? '#1976d2' : '#c62828', fontWeight: 500 }}>{c ? c.erp_id : m.missingErp}</td>
              <td style={p1td}>{c ? c.name : <span style={{ color: '#c62828', display: 'inline-flex', alignItems: 'center', gap: 4 }}><Icon name="error" size={15}/>Not found in SAP</span>}</td>
              <td style={p1td}>{m.qty}</td><td style={p1td}><span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, height: 22, padding: '0 9px', borderRadius: 11, fontSize: 12, fontWeight: 500, background: m.mandatory ? 'rgba(0,0,0,0.06)' : 'rgba(2,136,209,0.1)', color: m.mandatory ? 'rgba(0,0,0,0.7)' : '#01579b' }}><Icon name={m.mandatory ? 'push_pin' : 'tune'} size={13}/>{m.mandatory ? 'Static' : 'Dynamic'}</span></td>
              {P1_F.map(f => <td key={f} style={{ ...p1td, textAlign: 'center' }}>{c ? <StockChip s={p1StockStatus(c, f)} small/> : '—'}</td>)}
            </tr>); })}</tbody>
        </table>
      </div>
    </P1Card>
  );
}

function P1SkuProcesses({ s, onOpenProc }) {
  const S = useP1Store();
  const list0 = s.processes.slice();
  const cf = useColFilters(list0, {
    name: { type: 'text', get: m => S.procs[m.procId].name },
    type: { type: 'enum', get: m => S.procs[m.procId].type, options: ['Machine', 'Work'] }, qty: { type: 'number', get: m => m.qty },
    ...Object.fromEntries(P1_F.map(f => [f, { type: 'number', get: m => S.procs[m.procId].factories[f].runs ? S.procs[m.procId].factories[f].capacity : null }])),
  });
  const list = cf.rows;
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
    <P1Card title="Mapped processes" right={<ReadOnlyNote text="Read only · Source: Info file / Supplier"/>} pad={0}>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><FilterTh k="name" label="Process" cf={cf}/><FilterTh k="type" label="Type" cf={cf}/><FilterTh k="qty" label="Quantity required" cf={cf}/>{P1_F.map(f => <FilterTh key={f} k={f} label={`${f} %`} cf={cf} align="center"/>)}</tr></thead>
          <tbody>{list.map(m => { const p = S.procs[m.procId]; return (
            <tr key={m.step} onClick={() => onOpenProc(p.id)} style={{ cursor: 'pointer' }}>
              <td style={{ ...p1td, fontWeight: 500 }}>{p.name}</td><td style={p1td}><TypeChip t={p.type}/></td><td style={{ ...p1td, ...p1mono, fontWeight: 500 }}>{p1Qty(m.qty, p.uom)}</td>
              {P1_F.map(f => { const x = p.factories[f]; return <td key={f} style={{ ...p1td, textAlign: 'center' }}>{x.runs ? <span style={{ fontWeight: x.capacity < 100 ? 600 : 400, color: capColor(x.capacity) }}>{x.capacity}%</span> : <span style={{ color: 'rgba(0,0,0,0.3)' }} title="Doesn't run here">—</span>}</td>; })}
            </tr>); })}</tbody>
        </table>
      </div>
    </P1Card>
    {s.conflicts.length > 0 && <P1Card title={<span style={{ display: 'inline-flex', alignItems: 'center', gap: 10 }}>Processes used but not listed <ConflictChip/></span>} sub="From the TG Supplier production log sent with each item. Fix the mapping in the source, or confirm the floor used the wrong process." right={<ReadOnlyNote text="Read only · Source: TG Supplier production log"/>} pad={0}>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><th style={p1th}>Process used</th><th style={p1th}>Type</th><th style={p1th}>Machine used</th><th style={p1th}>Order ID</th><th style={p1th}>Factory</th><th style={p1th}>Day</th></tr></thead>
          <tbody>{s.conflicts.map((c, i) => { const p = S.procs[c.procId]; return (
            <tr key={i} onClick={() => p && onOpenProc(p.id)} style={{ cursor: 'pointer', background: 'rgba(237,108,2,0.04)' }}>
              <td style={{ ...p1td, fontWeight: 500 }}>{p ? p.name : c.procId}</td>
              <td style={p1td}>{p && <TypeChip t={p.type}/>}</td>
              <td style={p1td}>{c.machine}</td>
              <td style={{ ...p1td, ...p1mono, color: '#1976d2' }}>{c.orderId}</td>
              <td style={p1td}><FactoryTag f={c.factory}/></td>
              <td style={{ ...p1td, whiteSpace: 'nowrap' }}>{c.day}</td>
            </tr>); })}</tbody>
        </table>
      </div>
    </P1Card>}
    </div>
  );
}

/* Share editing — shared by SKU Factories tab and Factory share */
function p1SetProducible(sku, f, v, who) {
  const old = sku.share[f].producible; sku.share[f].producible = v; if (!v) sku.share[f].override = null;
  sku.history.unshift({ who, when: P1_NOW, field: `Producible · ${f}`, old: old ? 'Yes' : 'No', nw: v ? 'Yes' : 'No' });
  p1Log('SKU', sku.site_sku, who, `Producible · ${f}`, old ? 'Yes' : 'No', v ? 'Yes' : 'No'); p1Bump();
}
function p1SetOverride(sku, f, v, who) {
  const old = sku.share[f].override; sku.share[f].override = v;
  sku.history.unshift({ who, when: P1_NOW, field: `Share · ${f}`, old: old == null ? 'default' : old + '%', nw: v == null ? 'default' : v + '%' });
  p1Log('SKU', sku.site_sku, who, `Share · ${f}`, old == null ? 'default' : old + '%', v == null ? 'default' : v + '%'); p1Bump();
}

function ShareCell({ sku, f, eff, editable, who, compact }) {
  const sh = sku.share[f]; const ov = sh.override != null;
  const [draft, setDraft] = useStateP1S(null);
  const commit = (raw) => { setDraft(null); if (raw === '' || raw == null) return p1SetOverride(sku, f, null, who); const v = Math.max(0, Math.min(100, parseInt(raw, 10))); if (!isNaN(v)) p1SetOverride(sku, f, v, who); };
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, justifyContent: compact ? 'center' : 'flex-start' }}>
      {editable ? <input type="checkbox" checked={sh.producible} onChange={e => p1SetProducible(sku, f, e.target.checked, who)} title="Producible"/> : <Icon name={sh.producible ? 'check_box' : 'check_box_outline_blank'} size={18} sx={{ color: sh.producible ? 'rgba(0,0,0,0.6)' : 'rgba(0,0,0,0.25)' }}/>}
      {sh.producible ? (editable
        ? <input type="number" min="0" max="100" value={draft != null ? draft : eff} onChange={e => setDraft(e.target.value)} onBlur={e => commit(e.target.value)} onKeyDown={e => e.key === 'Enter' && e.target.blur()}
            style={{ ...p1input, width: 62, textAlign: 'right', fontWeight: 500, color: ov ? '#1565c0' : 'rgba(0,0,0,0.5)', background: ov ? 'rgba(25,118,210,0.08)' : '#fafafa', borderColor: ov ? 'rgba(25,118,210,0.5)' : 'rgba(0,0,0,0.2)' }}/>
        : <span style={{ minWidth: 44, textAlign: 'right', fontWeight: 500, padding: '3px 7px', borderRadius: 4, color: ov ? '#1565c0' : 'rgba(0,0,0,0.5)', background: ov ? 'rgba(25,118,210,0.08)' : 'transparent' }}>{eff}%</span>)
        : <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.3)', minWidth: 44 }}>—</span>}
    </div>
  );
}
function TotalCheck({ eff, sku }) {
  const any = P1_F.some(f => sku.share[f].producible); const tot = P1_F.reduce((n, f) => n + eff[f], 0);
  if (!any) return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 12, color: '#c62828' }}><Icon name="error" size={15}/>No factory</span>;
  return tot === 100 ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 12, color: '#2e7d32' }}><Icon name="check_circle" size={15}/>100%</span>
    : <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 12, color: '#c62828', fontWeight: 500 }}><Icon name="error" size={15}/>{tot}%</span>;
}
const resetRow = (sku, ctx) => { P1_F.filter(f => P1_CAN.editShare(ctx, f) && sku.share[f].override != null).forEach(f => p1SetOverride(sku, f, null, ctx.user.name)); ctx.toast(`${sku.site_sku} reset to default share`); };

function P1SkuFactories({ s }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const eff = p1EffectiveShare(s, S.defaultShare);
  const anyEditable = P1_F.some(f => P1_CAN.editShare(ctx, f));
  const hasOv = P1_F.some(f => P1_CAN.editShare(ctx, f) && s.share[f].override != null);
  return (
    <P1Card title="Factories" sub="Producible on/off and share per factory. Grey = default balance, blue = override." right={<div style={{ display: 'flex', gap: 10, alignItems: 'center' }}><TotalCheck eff={eff} sku={s}/>{anyEditable ? <Button variant="outlined" size="small" startIcon="restart_alt" disabled={!hasOv} onClick={() => resetRow(s, ctx)}>Reset to default</Button> : <ReadOnlyNote text="Factory managers edit their own factory"/>}</div>} pad={0}>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead><tr><th style={p1th}>Factory</th><th style={p1th}>Producible · share</th><th style={p1th}>Source</th><th style={p1th}>Default balance</th></tr></thead>
        <tbody>{P1_F.map(f => { const ov = s.share[f].override != null; return (
          <tr key={f}>
            <td style={p1td}><FactoryTag f={f}/></td>
            <td style={p1td}><ShareCell sku={s} f={f} eff={eff[f]} editable={P1_CAN.editShare(ctx, f)} who={ctx.user.name}/></td>
            <td style={p1td}>{s.share[f].producible ? <span style={{ fontSize: 11, padding: '2px 8px', borderRadius: 3, background: ov ? 'rgba(25,118,210,0.1)' : 'rgba(0,0,0,0.06)', color: ov ? '#1565c0' : 'rgba(0,0,0,0.55)', fontWeight: 500 }}>{ov ? 'Override' : 'Default'}</span> : <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.4)' }}>Not producible</span>}</td>
            <td style={{ ...p1td, ...p1mono, color: 'rgba(0,0,0,0.5)' }}>{S.defaultShare[f]}%</td>
          </tr>); })}</tbody>
      </table>
    </P1Card>
  );
}

/* ================= Factory share ================= */
function P1FactoryShare({ onOpen }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const [q, setQ] = useStateP1S(''); const [saved, setSaved] = useStateP1S(null);
  const canDef = P1_CAN.editDefaultShare(ctx);
  const rows = S.skuOrder.map(id => S.skus[id]).filter(s => s.type === 'Manufactured' && (!q || (s.name + s.site_sku).toLowerCase().includes(q.toLowerCase())));
  const defTot = P1_F.reduce((n, f) => n + S.defaultShare[f], 0);
  const editableF = P1_F.filter(f => P1_CAN.editShare(ctx, f));
  React.useEffect(() => { setSaved(P1_NOW.split(' · ')[1]); }, [S.audit.length]);
  const setDef = (f, v) => { const old = S.defaultShare[f]; S.defaultShare[f] = Math.max(0, Math.min(100, parseInt(v, 10) || 0)); p1Log('Default balance', f, ctx.user.name, `Default share · ${f}`, old + '%', S.defaultShare[f] + '%'); p1Bump(); };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 14, flexWrap: 'wrap', fontSize: 13 }}>
        {editableF.length ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><Icon name="edit" size={16} sx={{ color: '#1976d2' }}/>You can edit the <b style={{ fontWeight: 500 }}>{editableF.join(', ')}</b> column.</span>
          : <ReadOnlyNote text={canDef ? 'You edit the default balance only' : 'Read only for your role'}/>}
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><span style={{ width: 14, height: 14, borderRadius: 3, background: '#fafafa', border: '1px solid rgba(0,0,0,0.2)' }}></span>Default</span>
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><span style={{ width: 14, height: 14, borderRadius: 3, background: 'rgba(25,118,210,0.12)', border: '1px solid rgba(25,118,210,0.5)' }}></span>Override</span>
        <div style={{ flex: 1 }}></div>
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, color: '#2e7d32', fontSize: 12 }}><Icon name="cloud_done" size={16}/>Auto-saved{saved ? ` · ${saved}` : ''}</span>
      </div>
      <div style={p1card}>
        <div style={{ padding: '12px 16px', borderBottom: '1px solid rgba(0,0,0,0.08)', display: 'flex', gap: 10 }}><P1Search value={q} onChange={setQ} placeholder="Search product or Site SKU…"/></div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead><tr><th style={p1th}>Product</th>{P1_F.map(f => <th key={f} style={{ ...p1th, textAlign: 'center', background: editableF.includes(f) ? 'rgba(25,118,210,0.08)' : p1th.background, color: editableF.includes(f) ? '#1565c0' : p1th.color }}>{f} · producible / share</th>)}<th style={p1th}>Total</th><th style={p1th}></th></tr></thead>
            <tbody>
              <tr style={{ background: 'rgba(0,0,0,0.025)' }}>
                <td style={{ ...p1td, fontWeight: 500 }}>Default balance <span style={{ fontSize: 11, color: 'rgba(0,0,0,0.5)', fontWeight: 400 }}>· global</span></td>
                {P1_F.map(f => <td key={f} style={{ ...p1td, textAlign: 'center' }}>{canDef ? <input type="number" min="0" max="100" value={S.defaultShare[f]} onChange={e => setDef(f, e.target.value)} style={{ ...p1input, width: 64, textAlign: 'right', fontWeight: 600 }}/> : <b style={{ fontWeight: 600 }}>{S.defaultShare[f]}%</b>}</td>)}
                <td style={p1td}>{defTot === 100 ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 12, color: '#2e7d32' }}><Icon name="check_circle" size={15}/>100%</span> : <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 12, color: '#c62828', fontWeight: 500 }}><Icon name="error" size={15}/>{defTot}%</span>}</td>
                <td style={p1td}>{!canDef && <Icon name="lock" size={15} sx={{ color: 'rgba(0,0,0,0.35)' }}/>}</td>
              </tr>
              {rows.map(s => { const eff = p1EffectiveShare(s, S.defaultShare); const canReset = P1_F.some(f => P1_CAN.editShare(ctx, f) && s.share[f].override != null); return (
                <tr key={s.id}>
                  <td style={p1td}><div style={{ fontWeight: 500, cursor: 'pointer' }} onClick={() => onOpen(s.id)}>{s.name}</div><div style={{ ...p1mono, fontSize: 11.5, color: 'rgba(0,0,0,0.5)' }}>{s.site_sku}</div></td>
                  {P1_F.map(f => <td key={f} style={{ ...p1td, background: editableF.includes(f) ? 'rgba(25,118,210,0.03)' : 'transparent' }}><ShareCell sku={s} f={f} eff={eff[f]} editable={P1_CAN.editShare(ctx, f)} who={ctx.user.name} compact/></td>)}
                  <td style={p1td}><TotalCheck eff={eff} sku={s}/></td>
                  <td style={{ ...p1td, textAlign: 'right' }}>{editableF.length > 0 && <Button variant="text" size="small" startIcon="restart_alt" disabled={!canReset} onClick={() => resetRow(s, ctx)}>Reset to default</Button>}</td>
                </tr>); })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { P1ProductsList, P1SkuDetail, P1FactoryShare, SkuTypeChip });

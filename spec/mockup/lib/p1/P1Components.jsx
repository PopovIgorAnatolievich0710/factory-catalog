/* global React, Icon, IconButton, Button, Chip, Switch, COMPONENT_FAMILIES, P1_F, P1_STORE, P1_CAN, P1_NOW, useP1Store, useP1Ctx, p1Bump, p1Log,
   p1CritFor, p1StockStatus, p1WorstStatus, p1SkusUsingComp, p1ExportCsv, p1th, p1td, p1mono, p1card, p1select, p1input,
   SourceBadge, StockChip, FactoryTag, ActionRequired, ReadOnlyNote, P1Tabs, P1Back, P1Search, SyncStamp, HistoryTable, P1Field, P1Card */
const { useState: useStateP1C, useMemo: useMemoP1C } = React;

const famLabel = (id) => (COMPONENT_FAMILIES.find(f => f.id === id) || { label: id }).label;
const famOfP1 = (id) => COMPONENT_FAMILIES.find(f => f.id === id) || { label: id, color: '#607d8b', icon: 'category' };
const totalOf = (c, f) => c.stock[f].wh + c.stock[f].floor;

const COMP_COLS = [
  { k: 'erp', l: 'ERP ID' }, { k: 'name', l: 'Name' }, { k: 'family', l: 'Family' }, { k: 'material', l: 'Material' },
  { k: 'status', l: 'Status' }, { k: 'total', l: 'Total stock' }, { k: 'crit', l: 'Critical level' }, { k: 'cost', l: 'Unit cost' },
  { k: 'KG', l: 'KG stock' }, { k: 'NZ', l: 'NZ stock' }, { k: 'HU', l: 'HU stock' }, { k: 'TH', l: 'TH stock' }, { k: 'skus', l: 'SKUs using' },
];
const COMP_COLS_DEFAULT = ['erp', 'name', 'family', 'status', 'total', 'crit', 'skus'];

function P1ComponentsList({ onOpen }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const [q, setQ] = useStateP1C('');
  const [cols, setCols] = useStateP1C(() => { try { const v = JSON.parse(localStorage.getItem('p1.compCols')); if (Array.isArray(v) && v.length) return v; } catch (e) {} return COMP_COLS_DEFAULT; });
  const [picker, setPicker] = useStateP1C(false);
  const saveCols = (v) => { setCols(v); try { localStorage.setItem('p1.compCols', JSON.stringify(v)); } catch (e) {} };
  const base = Object.values(S.comps).map(c => ({ c, w: p1WorstStatus(c), total: P1_F.reduce((n, f) => n + totalOf(c, f), 0) }))
    .filter(({ c }) => !q || c.erp_id.toLowerCase().includes(q.toLowerCase()) || c.name.toLowerCase().includes(q.toLowerCase()));
  const defs = {
    erp: { type: 'text', get: r => r.c.erp_id }, name: { type: 'text', get: r => r.c.name },
    family: { type: 'enum', get: r => famLabel(r.c.family) }, material: { type: 'enum', get: r => r.c.material },
    status: { type: 'enum', get: r => P1_STATUS_LABEL[r.w.status], options: ['OK', 'Low', 'OOS', 'OOS conflict'] },
    total: { type: 'number', get: r => r.total }, crit: { type: 'number', get: r => r.c.critical_global },
    cost: { type: 'number', get: r => r.c.unit_cost }, skus: { type: 'number', get: r => p1SkusUsingComp(r.c.id).length },
    KG: { type: 'number', get: r => totalOf(r.c, 'KG') }, NZ: { type: 'number', get: r => totalOf(r.c, 'NZ') },
    HU: { type: 'number', get: r => totalOf(r.c, 'HU') }, TH: { type: 'number', get: r => totalOf(r.c, 'TH') },
  };
  const cf = useColFilters(base, defs);
  const rows = cf.rows;
  const shown = COMP_COLS.filter(x => cols.includes(x.k));
  const cell = (k, { c, w, total }) => {
    switch (k) {
      case 'erp': return <span style={{ ...p1mono, color: '#1976d2', fontWeight: 500 }}>{c.erp_id}</span>;
      case 'name': return <span style={{ fontWeight: 500 }}>{c.name}</span>;
      case 'family': return famLabel(c.family);
      case 'material': return <span style={{ fontSize: 13, color: 'rgba(0,0,0,0.7)' }}>{c.material}</span>;
      case 'status': return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}><StockChip s={w.status} small/>{w.status !== 'OK' && <span style={{ fontSize: 11, color: 'rgba(0,0,0,0.55)' }}>{w.at.join(' · ')}</span>}</span>;
      case 'total': return <span style={p1mono}>{total.toLocaleString()}</span>;
      case 'crit': return <span style={p1mono}>{c.critical_global.toLocaleString()}{Object.keys(c.critical_override).length > 0 && <span title="Has factory overrides" style={{ color: '#1976d2', marginLeft: 4 }}>*</span>}</span>;
      case 'cost': return <span style={p1mono}>${c.unit_cost.toFixed(2)}</span>;
      case 'skus': return p1SkusUsingComp(c.id).length;
      default: return <span style={{ ...p1mono, color: p1StockStatus(c, k) === 'OK' ? 'inherit' : '#c62828' }}>{totalOf(c, k).toLocaleString()}</span>;
    }
  };
  const doExport = () => {
    p1ExportCsv('components.csv', shown.map(x => x.l), rows.map(r => shown.map(x => { const v = defs[x.k].get(r); return v; })));
    ctx.toast(`Exported ${rows.length} components to Excel`);
  };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <SyncStamp label="Last SAP sync" when={S.sync.sap.last}/>
        <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.5)' }}>Components are created in SAP only.</span>
      </div>
      <div style={{ ...p1card }}>
        <div style={{ padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10, borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
          <P1Search value={q} onChange={setQ} placeholder="Search ERP ID or name…"/>
          <div style={{ flex: 1 }}></div>
          <div style={{ position: 'relative' }}>
            <Button variant="outlined" startIcon="view_column" onClick={() => setPicker(!picker)}>Columns · {cols.length}/8</Button>
            {picker && <>
              <div onClick={() => setPicker(false)} style={{ position: 'fixed', inset: 0, zIndex: 10 }}></div>
              <div style={{ position: 'absolute', right: 0, top: '110%', zIndex: 11, background: '#fff', borderRadius: 8, boxShadow: '0 8px 24px rgba(0,0,0,0.18)', padding: 8, width: 230 }}>
                <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.55)', padding: '4px 8px 8px' }}>Up to 8 columns · remembered</div>
                {COMP_COLS.map(x => { const on = cols.includes(x.k); const dis = !on && cols.length >= 8; return (
                  <label key={x.k} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '5px 8px', fontSize: 13, cursor: dis ? 'not-allowed' : 'pointer', opacity: dis ? 0.4 : 1 }}>
                    <input type="checkbox" checked={on} disabled={dis || (on && cols.length === 1)} onChange={() => saveCols(on ? cols.filter(k => k !== x.k) : COMP_COLS.filter(y => cols.includes(y.k) || y.k === x.k).map(y => y.k))}/>{x.l}
                  </label>); })}
                <div style={{ borderTop: '1px solid rgba(0,0,0,0.08)', marginTop: 6, paddingTop: 6 }}><Button variant="text" size="small" onClick={() => saveCols(COMP_COLS_DEFAULT)}>Reset to default</Button></div>
              </div>
            </>}
          </div>
          <Button variant="outlined" startIcon="download" onClick={doExport}>Export to Excel</Button>
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead><tr>{shown.map(x => <FilterTh key={x.k} k={x.k} label={x.l} cf={cf}/>)}<th style={p1th}></th></tr></thead>
            <tbody>
              {rows.map(r => { const act = r.w.status === 'OOS' || r.w.status === 'OOS_CONFLICT'; return (
                <tr key={r.c.id} onClick={() => onOpen(r.c.id)} style={{ cursor: 'pointer', background: act ? 'rgba(211,47,47,0.025)' : 'transparent' }}
                  onMouseEnter={e => e.currentTarget.style.background = 'rgba(0,0,0,0.03)'} onMouseLeave={e => e.currentTarget.style.background = act ? 'rgba(211,47,47,0.025)' : 'transparent'}>
                  {shown.map((x, i) => <td key={x.k} style={{ ...p1td, borderLeft: i === 0 && act ? '3px solid #d32f2f' : undefined }}>{cell(x.k, r)}</td>)}
                  <td style={{ ...p1td, textAlign: 'right', whiteSpace: 'nowrap' }}>{act && <ActionRequired/>}</td>
                </tr>); })}
              {!rows.length && <tr><td colSpan={shown.length + 1} style={{ ...p1td, textAlign: 'center', padding: 28, color: 'rgba(0,0,0,0.5)' }}>No components match these filters.</td></tr>}
            </tbody>
          </table>
        </div>
        <FilterSummary cf={cf} total={Object.keys(S.comps).length} noun="components"/>
      </div>
    </div>
  );
}
const P1_STATUS_LABEL = { OK: 'OK', LOW: 'Low', OOS: 'OOS', OOS_CONFLICT: 'OOS conflict' };

/* ================= Component detail ================= */
function P1ComponentDetail({ id, onBack, onOpenSku, onOpenComp }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const c = S.comps[id];
  const [tab, setTab] = useStateP1C('spec');
  if (!c) return null;
  const fm = famOfP1(c.family); const w = p1WorstStatus(c);
  const skus = p1SkusUsingComp(c.id);
  const skuRows = skus.map(sk => ({ s: sk, m: sk.components.find(x => x.compId === c.id) }));
  const skuCf = useColFilters(skuRows, { name: { type: 'text', get: r => r.s.name }, site: { type: 'text', get: r => r.s.site_sku }, qty: { type: 'number', get: r => r.m.qty }, usage: { type: 'enum', get: r => r.m.mandatory ? 'Static' : 'Dynamic', options: ['Static', 'Dynamic'] } });
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <P1Back label="Components" onClick={onBack} crumb={c.erp_id}/>
      <div style={{ ...p1card, padding: 22, display: 'flex', gap: 18, alignItems: 'center', flexWrap: 'wrap' }}>
        <span style={{ width: 56, height: 56, borderRadius: 10, background: `${fm.color}18`, color: fm.color, display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}><Icon name={fm.icon} size={30}/></span>
        <div style={{ flex: 1, minWidth: 240 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4, flexWrap: 'wrap' }}><span style={{ ...p1mono, color: '#1976d2', fontWeight: 500 }}>{c.erp_id}</span><span style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>· {fm.label}</span></div>
          <h2 style={{ margin: 0, fontSize: 22, fontWeight: 400 }}>{c.name}</h2>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>{P1_F.map(f => <span key={f} style={{ display: 'inline-flex', flexDirection: 'column', alignItems: 'center', gap: 4 }}><span style={{ fontSize: 11, color: 'rgba(0,0,0,0.55)', fontWeight: 600 }}>{f}</span><StockChip s={p1StockStatus(c, f)} small/></span>)}</div>
      </div>
      <P1Tabs value={tab} onChange={setTab} tabs={[
        { id: 'spec', label: 'Specifications' }, { id: 'stock', label: 'Stock by factory' }, { id: 'alts', label: 'Alternatives', count: c.alternatives.length },
        { id: 'skus', label: 'SKUs using this', count: skus.length }, { id: 'settings', label: 'Settings' }, { id: 'history', label: 'Audit' }]}/>
      {tab === 'spec' && <P1Card title="Specifications" right={<ReadOnlyNote text="Read only · Source: SAP"/>} pad={0}>
        <table style={{ width: '100%', maxWidth: 760, borderCollapse: 'collapse' }}>
          <tbody>
            {[
              ['ERP ID', <span style={{ ...p1mono, fontSize: 14 }}>{c.erp_id}</span>],
              ['Name', c.name],
              ['Family', <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><Icon name={fm.icon} size={16} sx={{ color: fm.color }}/>{fm.label}</span>],
              ['Material', c.material],
              ['Measure', c.measure ? `${c.measure.value} ${c.measure.unit}` : '—'],
              ['Measured by', c.measure ? c.measure.method.charAt(0).toUpperCase() + c.measure.method.slice(1) : '—'],
              ['Unit of measure', c.uom],
              ['Unit cost', <span style={{ ...p1mono, fontSize: 14 }}>${c.unit_cost.toFixed(2)} {c.currency}</span>],
            ].map(([l, v], i) => (
              <tr key={l} style={{ background: i % 2 ? '#fafbfc' : '#fff' }}>
                <th style={{ textAlign: 'left', width: 200, padding: '12px 18px', fontSize: 13, fontWeight: 500, color: 'rgba(0,0,0,0.6)', borderBottom: '1px solid rgba(0,0,0,0.06)', verticalAlign: 'top' }}>{l}</th>
                <td style={{ padding: '12px 18px', fontSize: 14, color: 'rgba(0,0,0,0.87)', borderBottom: '1px solid rgba(0,0,0,0.06)' }}>{v}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </P1Card>}
      {tab === 'stock' && <P1StockByFactory c={c}/>}
      {tab === 'alts' && <P1Alternatives c={c} onOpenComp={onOpenComp}/>}
      {tab === 'skus' && <P1Card title="SKUs using this component" right={<ReadOnlyNote text="Read only · Source: Master catalog"/>} pad={0}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><FilterTh k="name" label="Product" cf={skuCf}/><FilterTh k="site" label="Site SKU" cf={skuCf}/><FilterTh k="qty" label="Qty" cf={skuCf}/><FilterTh k="usage" label="Usage" cf={skuCf}/></tr></thead>
          <tbody>{skuCf.rows.map(({ s, m }) => { return (
            <tr key={s.id} onClick={() => onOpenSku(s.id)} style={{ cursor: 'pointer' }}><td style={{ ...p1td, fontWeight: 500 }}>{s.name}</td><td style={{ ...p1td, ...p1mono }}>{s.site_sku}</td><td style={p1td}>{m.qty}</td><td style={p1td}><UsageChip dynamic={!m.mandatory}/></td></tr>); })}
            {!skuCf.rows.length && <tr><td colSpan="4" style={{ ...p1td, textAlign: 'center', padding: 24, color: 'rgba(0,0,0,0.5)' }}>No SKUs map this component.</td></tr>}
          </tbody>
        </table>
      </P1Card>}
      {tab === 'settings' && <P1CompSettings c={c}/>}
      {tab === 'history' && <HistoryTable rows={c.history}/>}
    </div>
  );
}

function P1StockByFactory({ c }) {
  const ctx = useP1Ctx();
  const [notes, setNotes] = useStateP1C({});
  const [editing, setEditing] = useStateP1C({});
  const saveNote = (f) => {
    const m = c.manual_oos[f]; if (!m) return; const old = m.note || '—';
    m.note = (notes[f] || '').trim(); setEditing({ ...editing, [f]: false });
    if (m.note) { c.history.unshift({ who: ctx.user.name, when: P1_NOW, field: `OOS note · ${f}`, old, nw: m.note }); ctx.toast('Note saved'); }
    p1Bump();
  };
  const toggle = (f, on) => {
    const prev = c.manual_oos[f];
    if (on) { c.manual_oos[f] = { on: true, note: '', by: ctx.user.name, at: P1_NOW }; setNotes({ ...notes, [f]: '' }); setEditing({ ...editing, [f]: true }); }
    else { delete c.manual_oos[f]; setEditing({ ...editing, [f]: false }); }
    c.history.unshift({ who: ctx.user.name, when: P1_NOW, field: `Manual OOS · ${f}`, old: prev ? 'On' : 'Off', nw: on ? 'On' : 'Off' });
    p1Log('Component', c.erp_id, ctx.user.name, `Manual OOS · ${f}`, prev ? 'On' : 'Off', on ? 'On' : 'Off');
    p1Bump(); ctx.toast(on ? `${c.erp_id} marked out of stock at ${f}` : `Out-of-stock flag cleared at ${f}`);
  };
  return (
    <P1Card title="Stock by factory" right={<ReadOnlyNote text="Source: SAP. Factory manager edits."/>} pad={0}>
      <div style={{ overflowX: 'auto' }}>
      <table style={{ width: '100%', minWidth: 1080, borderCollapse: 'collapse', tableLayout: 'fixed' }}>
        <colgroup><col style={{ width: 72 }}/><col style={{ width: 104 }}/><col style={{ width: 104 }}/><col style={{ width: 84 }}/><col style={{ width: 170 }}/><col style={{ width: 130 }}/><col style={{ width: 110 }}/><col/></colgroup>
        <thead><tr><th style={p1th}>Factory</th><th style={p1th}>Warehouse</th><th style={p1th}>Floor</th><th style={p1th}>Total</th><th style={p1th}>Critical level</th><th style={p1th}>Status</th><th style={p1th}>OOS flag</th><th style={p1th}>Note</th></tr></thead>
        <tbody>{P1_F.map(f => {
          const s = c.stock[f], tot = s.wh + s.floor, crit = p1CritFor(c, f), ov = c.critical_override[f] != null;
          const st = p1StockStatus(c, f), m = c.manual_oos[f], can = P1_CAN.markOOS(ctx, f);
          const cell = { ...p1td, height: 64, overflow: 'hidden' };
          return (
            <tr key={f} style={{ background: st === 'OOS_CONFLICT' ? 'rgba(211,47,47,0.03)' : 'transparent' }}>
              <td style={cell}><FactoryTag f={f}/></td>
              <td style={{ ...cell, ...p1mono }}>{s.wh.toLocaleString()}</td>
              <td style={{ ...cell, ...p1mono }}>{s.floor.toLocaleString()}</td>
              <td style={{ ...cell, ...p1mono, fontWeight: 600 }}>{tot.toLocaleString()}</td>
              <td style={cell}><span style={p1mono}>{crit.toLocaleString()}</span> <span style={{ fontSize: 11, padding: '1px 6px', borderRadius: 3, marginLeft: 4, background: ov ? 'rgba(25,118,210,0.1)' : 'rgba(0,0,0,0.06)', color: ov ? '#1565c0' : 'rgba(0,0,0,0.55)' }}>{ov ? 'override' : 'global'}</span></td>
              <td style={cell}><span title={st === 'OOS_CONFLICT' ? 'Stock is above the critical level but the manual flag is still on' : ''}><StockChip s={st}/></span></td>
              <td style={cell}>
                <OOSFlag checked={!!m} active={can} onChange={v => toggle(f, v)}/>
              </td>
              <td style={cell}>
                {m && can && editing[f] ? (
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <input autoFocus value={notes[f] || ''} onChange={e => setNotes({ ...notes, [f]: e.target.value })} onKeyDown={e => e.key === 'Enter' && saveNote(f)} placeholder="Optional note" style={{ ...p1input, flex: 1, minWidth: 0, boxSizing: 'border-box' }}/>
                    <button onClick={() => saveNote(f)} title="Save note" style={{ width: 30, height: 30, flexShrink: 0, borderRadius: 4, border: 'none', background: '#2e7d32', color: '#fff', cursor: 'pointer', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}><Icon name="check" size={18}/></button>
                  </div>
                ) : m ? (
                  <div onClick={() => { if (can) { setNotes({ ...notes, [f]: m.note || '' }); setEditing({ ...editing, [f]: true }); } }} title={can ? 'Click to edit the note' : m.note} style={{ display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0, cursor: can ? 'text' : 'default' }}>
                    <span title={m.note} style={{ fontSize: 13, color: m.note ? 'rgba(0,0,0,0.85)' : 'rgba(0,0,0,0.4)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{m.note ? `“${m.note}”` : 'No note'}</span>
                    <span style={{ fontSize: 11.5, color: 'rgba(0,0,0,0.5)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>Set by {m.by} · {m.at}</span>
                  </div>
                ) : <span style={{ fontSize: 13, color: 'rgba(0,0,0,0.3)' }}>—</span>}
              </td>
            </tr>); })}
        </tbody>
      </table>
      </div>
    </P1Card>
  );
}

function OOSFlag({ checked, active, onChange }) {
  const box = checked ? (active ? '#d32f2f' : 'rgba(211,47,47,0.45)') : 'transparent';
  const border = checked ? box : active ? 'rgba(0,0,0,0.6)' : 'rgba(0,0,0,0.22)';
  return (
    <label title={active ? '' : 'Only this factory’s manager can change it'} style={{ display: 'inline-flex', alignItems: 'center', gap: 8, cursor: active ? 'pointer' : 'not-allowed', userSelect: 'none' }}>
      <span onClick={() => active && onChange(!checked)} style={{ width: 18, height: 18, borderRadius: 3, border: `2px solid ${border}`, background: box, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color: '#fff', boxSizing: 'border-box', transition: '120ms' }}>
        {checked && <Icon name="check" size={14} sx={{ fontWeight: 700 }}/>}
      </span>
      <span onClick={() => active && onChange(!checked)} style={{ fontSize: 13, fontWeight: checked ? 500 : 400, color: checked ? (active ? '#c62828' : 'rgba(198,40,40,0.6)') : (active ? 'rgba(0,0,0,0.75)' : 'rgba(0,0,0,0.38)') }}>OOS</span>
    </label>
  );
}

function UsageChip({ dynamic }) {
  return <span title={dynamic ? 'Used only when the order’s options call for it' : 'Used in every unit of this SKU'} style={{ display: 'inline-flex', alignItems: 'center', gap: 4, height: 22, padding: '0 9px', borderRadius: 11, fontSize: 12, fontWeight: 500, background: dynamic ? 'rgba(2,136,209,0.1)' : 'rgba(0,0,0,0.06)', color: dynamic ? '#01579b' : 'rgba(0,0,0,0.7)' }}><Icon name={dynamic ? 'tune' : 'push_pin'} size={13}/>{dynamic ? 'Dynamic' : 'Static'}</span>;
}

function AltCheck({ label, checked, active, onChange }) {
  const box = checked ? (active ? '#1976d2' : 'rgba(25,118,210,0.4)') : 'transparent';
  const border = checked ? box : active ? 'rgba(0,0,0,0.6)' : 'rgba(0,0,0,0.22)';
  return (
    <label onClick={() => active && onChange(!checked)} style={{ display: 'inline-flex', alignItems: 'center', gap: 7, cursor: active ? 'pointer' : 'not-allowed', userSelect: 'none', whiteSpace: 'nowrap' }}>
      <span style={{ width: 16, height: 16, borderRadius: 3, border: `2px solid ${border}`, background: box, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color: '#fff', boxSizing: 'border-box' }}>{checked && <Icon name="check" size={12}/>}</span>
      <span style={{ fontSize: 12.5, color: active ? 'rgba(0,0,0,0.8)' : 'rgba(0,0,0,0.45)' }}>{label}</span>
    </label>
  );
}

function P1Alternatives({ c, onOpenComp }) {
  const S = useP1Store(); const ctx = useP1Ctx(); const can = P1_CAN.editAlternatives(ctx);
  const [pick, setPick] = useStateP1C('');
  const avail = Object.values(S.comps).filter(x => x.id !== c.id && !c.alternatives.includes(x.id)).sort((a, b) => (a.family === c.family ? -1 : 0) - (b.family === c.family ? -1 : 0));
  const add = () => { const a = S.comps[pick]; if (!a) return; c.alternatives.push(pick); c.history.unshift({ who: ctx.user.name, when: P1_NOW, field: 'Alternatives', old: '—', nw: `+ ${a.erp_id}` }); p1Log('Component', c.erp_id, ctx.user.name, 'Alternatives', '—', `+ ${a.erp_id}`); setPick(''); p1Bump(); ctx.toast(`${a.erp_id} added as an alternative`); };
  if (!c.altUse) c.altUse = {};
  const useOf = (aid) => c.altUse[aid] || (c.altUse[aid] = { balancer: true, planner: c.alternatives.indexOf(aid) % 2 === 0 });
  const setUse = (aid, k, v) => { const u = useOf(aid); u[k] = v; const a = S.comps[aid]; c.history.unshift({ who: ctx.user.name, when: P1_NOW, field: `Alternative ${a.erp_id} · ${k === 'balancer' ? 'for balancer' : 'for S.C. planner'}`, old: v ? 'No' : 'Yes', nw: v ? 'Yes' : 'No' }); p1Bump(); };
  const remove = (aid) => { const a = S.comps[aid]; c.alternatives = c.alternatives.filter(x => x !== aid); c.history.unshift({ who: ctx.user.name, when: P1_NOW, field: 'Alternatives', old: a.erp_id, nw: 'removed' }); p1Bump(); ctx.toast(`${a.erp_id} removed`); };
  return (
    <P1Card title="Approved alternatives" right={!can && <ReadOnlyNote text="Supply chain manager edits"/>} pad={0}>
      <div style={{ overflowX: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead><tr><th style={p1th}>ERP ID</th><th style={p1th}>Name</th>{P1_F.map(f => <th key={f} style={p1th}>{f} stock</th>)}<th style={p1th}>Use</th>{can && <th style={p1th}></th>}</tr></thead>
        <tbody>
          {c.alternatives.map(aid => { const a = S.comps[aid]; return (
            <tr key={aid}>
              <td style={{ ...p1td, ...p1mono, color: '#1976d2', cursor: 'pointer' }} onClick={() => onOpenComp(aid)}>{a.erp_id}</td>
              <td style={{ ...p1td, fontWeight: 500 }}>{a.name}</td>
              {P1_F.map(f => <td key={f} style={p1td}><span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><span style={p1mono}>{totalOf(a, f).toLocaleString()}</span>{p1StockStatus(a, f) !== 'OK' && <StockChip s={p1StockStatus(a, f)} small/>}</span></td>)}
              <td style={p1td}><div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <AltCheck label="For balancer" checked={useOf(aid).balancer} active={can} onChange={v => setUse(aid, 'balancer', v)}/>
                <AltCheck label="For S.C. planner" checked={useOf(aid).planner} active={can} onChange={v => setUse(aid, 'planner', v)}/>
              </div></td>
              {can && <td style={{ ...p1td, textAlign: 'right' }}><IconButton name="delete_outline" size={18} onClick={() => remove(aid)}/></td>}
            </tr>); })}
          {!c.alternatives.length && <tr><td colSpan={can ? 8 : 7} style={{ ...p1td, textAlign: 'center', padding: 24, color: 'rgba(0,0,0,0.5)' }}>No approved alternatives.</td></tr>}
        </tbody>
      </table>
      </div>
      {can && <div style={{ display: 'flex', gap: 10, padding: '12px 16px', borderTop: '1px solid rgba(0,0,0,0.08)', alignItems: 'center', flexWrap: 'wrap' }}>
        <select value={pick} onChange={e => setPick(e.target.value)} style={{ ...p1select, minWidth: 320 }}><option value="">Choose a component to add…</option>{avail.map(a => <option key={a.id} value={a.id}>{a.erp_id} · {a.name}{a.family === c.family ? '' : ` (${famLabel(a.family)})`}</option>)}</select>
        <Button variant="outlined" size="small" startIcon="add" disabled={!pick} onClick={add}>Add alternative</Button>
      </div>}
    </P1Card>
  );
}

function P1CompSettings({ c }) {
  const ctx = useP1Ctx(); const can = P1_CAN.editCritical(ctx);
  const [g, setG] = useStateP1C(String(c.critical_global));
  const saveGlobal = () => { const v = parseInt(g, 10); if (isNaN(v) || v < 0) return; c.history.unshift({ who: ctx.user.name, when: P1_NOW, field: 'Critical level (global)', old: c.critical_global.toLocaleString(), nw: v.toLocaleString() }); c.critical_global = v; p1Bump(); ctx.toast('Global critical level saved'); };
  const setOv = (f, raw) => { const prev = c.critical_override[f]; if (raw === '' || raw == null) delete c.critical_override[f]; else { const v = parseInt(raw, 10); if (isNaN(v)) return; c.critical_override[f] = v; } c.history.unshift({ who: ctx.user.name, when: P1_NOW, field: `Critical level · ${f} override`, old: prev != null ? prev.toLocaleString() : '—', nw: c.critical_override[f] != null ? c.critical_override[f].toLocaleString() : '—' }); p1Bump(); };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <P1Card title="Global critical stock level" sub="Applies to every factory without an override." right={!can && <ReadOnlyNote text="Supply chain manager edits"/>}>
        {can ? <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}><input type="number" min="0" value={g} onChange={e => setG(e.target.value)} style={{ ...p1input, width: 140, fontSize: 15 }}/><span style={{ fontSize: 13, color: 'rgba(0,0,0,0.6)' }}>{c.uom}</span><Button variant="contained" size="small" disabled={String(c.critical_global) === g} onClick={saveGlobal}>Save</Button></div>
          : <div style={{ fontSize: 24, fontWeight: 300 }}>{c.critical_global.toLocaleString()} <span style={{ fontSize: 13, color: 'rgba(0,0,0,0.6)' }}>{c.uom}</span></div>}
      </P1Card>
      <P1Card title="Per-factory overrides" sub="Optional. When set, the override wins over the global level." pad={0}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><th style={p1th}>Factory</th><th style={p1th}>Override</th><th style={p1th}>Level that applies</th>{can && <th style={p1th}></th>}</tr></thead>
          <tbody>{P1_F.map(f => { const ov = c.critical_override[f]; return (
            <tr key={f}>
              <td style={p1td}><FactoryTag f={f}/></td>
              <td style={p1td}>{can ? <input type="number" min="0" placeholder="— none —" value={ov != null ? ov : ''} onChange={e => setOv(f, e.target.value)} style={{ ...p1input, width: 130 }}/> : <span style={{ ...p1mono, color: ov != null ? 'inherit' : 'rgba(0,0,0,0.4)' }}>{ov != null ? ov.toLocaleString() : '— none —'}</span>}</td>
              <td style={p1td}><span style={p1mono}>{p1CritFor(c, f).toLocaleString()}</span> <span style={{ fontSize: 11, color: ov != null ? '#1565c0' : 'rgba(0,0,0,0.5)', marginLeft: 6 }}>{ov != null ? 'override' : 'global'}</span></td>
              {can && <td style={{ ...p1td, textAlign: 'right' }}>{ov != null && <Button variant="text" size="small" onClick={() => setOv(f, '')}>Clear</Button>}</td>}
            </tr>); })}</tbody>
        </table>
      </P1Card>
    </div>
  );
}

/* ================= Component stock levels ================= */
function P1StockLevels({ onOpen }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const [q, setQ] = useStateP1C(''); const fac = 'all';
  const rows = [];
  Object.values(S.comps).forEach(c => (fac === 'all' ? P1_F : [fac]).forEach(f => {
    const st = p1StockStatus(c, f); if (st === 'OK') return;
    if (q) { const s = q.toLowerCase(); if (!c.erp_id.toLowerCase().includes(s) && !c.name.toLowerCase().includes(s)) return; }
    rows.push({ c, f, st });
  }));
  rows.sort((a, b) => P1_RANK_LOCAL[b.st] - P1_RANK_LOCAL[a.st]);
  const slCf = useColFilters(rows, {
    comp: { type: 'text', get: r => r.c.name + ' ' + r.c.erp_id }, f: { type: 'enum', get: r => r.f, options: P1_F },
    wh: { type: 'number', get: r => r.c.stock[r.f].wh }, fl: { type: 'number', get: r => r.c.stock[r.f].floor },
    tot: { type: 'number', get: r => totalOf(r.c, r.f) }, crit: { type: 'number', get: r => p1CritFor(r.c, r.f) },
    st: { type: 'enum', get: r => P1_STATUS_LABEL[r.st], options: ['Low', 'OOS', 'OOS conflict'] },
  });
  const counts = { LOW: rows.filter(r => r.st === 'LOW').length, OOS: rows.filter(r => r.st === 'OOS').length, OOS_CONFLICT: rows.filter(r => r.st === 'OOS_CONFLICT').length };
  const doExport = () => { p1ExportCsv('component-stock-levels.csv', ['ERP ID', 'Name', 'Factory', 'Warehouse', 'Production floor', 'Total', 'Critical level', 'Status'], slCf.rows.map(r => [r.c.erp_id, r.c.name, r.f, r.c.stock[r.f].wh, r.c.stock[r.f].floor, totalOf(r.c, r.f), p1CritFor(r.c, r.f), P1_STATUS_LABEL[r.st]])); ctx.toast(`Exported ${rows.length} rows to Excel`); };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <span style={{ fontSize: 14, color: 'rgba(0,0,0,0.7)' }}>Components below the critical level or out of stock, per factory.</span>
        <div style={{ flex: 1 }}></div><SyncStamp label="Last SAP sync" when={S.sync.sap.last}/>
      </div>
      <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
        {[['OOS_CONFLICT', counts.OOS_CONFLICT], ['OOS', counts.OOS], ['LOW', counts.LOW]].map(([k, n]) => <span key={k} style={{ display: 'inline-flex', alignItems: 'center', gap: 8, padding: '8px 14px', ...p1card }}><StockChip s={k} small/><b style={{ fontWeight: 500, fontSize: 18 }}>{n}</b></span>)}
      </div>
      <div style={p1card}>
        <div style={{ padding: '12px 16px', display: 'flex', gap: 10, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
          <P1Search value={q} onChange={setQ} placeholder="Search ERP ID or name…"/>
          <div style={{ flex: 1 }}></div>
          <Button variant="outlined" startIcon="download" onClick={doExport}>Export to Excel</Button>
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead><tr><FilterTh k="comp" label="Component" cf={slCf}/><FilterTh k="f" label="Factory" cf={slCf}/><FilterTh k="wh" label="Central warehouse" cf={slCf}/><FilterTh k="fl" label="Production floor" cf={slCf}/><FilterTh k="tot" label="Total" cf={slCf}/><FilterTh k="crit" label="Critical level" cf={slCf}/><FilterTh k="st" label="Status" cf={slCf}/><th style={p1th}>Manual flag</th></tr></thead>
            <tbody>{slCf.rows.map(r => { const m = r.c.manual_oos[r.f]; return (
              <tr key={r.c.id + r.f} onClick={() => onOpen(r.c.id)} style={{ cursor: 'pointer' }} onMouseEnter={e => e.currentTarget.style.background = 'rgba(0,0,0,0.03)'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                <td style={p1td}><div style={{ fontWeight: 500 }}>{r.c.name}</div><div style={{ ...p1mono, fontSize: 11.5, color: 'rgba(0,0,0,0.55)' }}>{r.c.erp_id}</div></td>
                <td style={p1td}><FactoryTag f={r.f}/></td>
                <td style={{ ...p1td, ...p1mono }}>{r.c.stock[r.f].wh.toLocaleString()}</td>
                <td style={{ ...p1td, ...p1mono }}>{r.c.stock[r.f].floor.toLocaleString()}</td>
                <td style={{ ...p1td, ...p1mono, fontWeight: 600 }}>{totalOf(r.c, r.f).toLocaleString()}</td>
                <td style={{ ...p1td, ...p1mono }}>{p1CritFor(r.c, r.f).toLocaleString()}</td>
                <td style={p1td}><StockChip s={r.st} small/></td>
                <td style={{ ...p1td, fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>{m ? `${m.by} · ${m.at.split(' · ')[0]}` : '—'}</td>
              </tr>); })}
              {!rows.length && <tr><td colSpan="8" style={{ ...p1td, textAlign: 'center', padding: 28, color: 'rgba(0,0,0,0.5)' }}>Nothing below the critical level.</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
const P1_RANK_LOCAL = { OK: 0, LOW: 1, OOS: 2, OOS_CONFLICT: 3 };

Object.assign(window, { P1ComponentsList, P1ComponentDetail, P1StockLevels });

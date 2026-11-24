/* global React, Icon, IconButton, Button, Chip, Avatar, FACTORIES, P1_F */
/* Shared Phase 1 UI primitives */
const P1Context = React.createContext({ role: 'floor_user', factory: 'NZ', user: {}, toast: () => {} });
function useP1Ctx() { return React.useContext(P1Context); }

const p1th = { textAlign: 'left', padding: '10px 14px', borderBottom: '1px solid rgba(0,0,0,0.12)', fontWeight: 500, fontSize: 12, color: 'rgba(0,0,0,0.6)', textTransform: 'uppercase', letterSpacing: '0.04em', whiteSpace: 'nowrap', background: '#fafafa' };
const p1td = { padding: '10px 14px', borderBottom: '1px solid rgba(0,0,0,0.06)', verticalAlign: 'middle', color: 'rgba(0,0,0,0.87)', fontSize: 14 };
const p1mono = { fontFamily: 'Roboto Mono, monospace', fontSize: 12.5 };
const p1card = { background: '#fff', borderRadius: 8, border: '1px solid rgba(0,0,0,0.12)' };
const p1select = { padding: '7px 10px', borderRadius: 4, border: '1px solid rgba(0,0,0,0.23)', background: '#fff', fontFamily: 'Roboto', fontSize: 13, color: 'rgba(0,0,0,0.87)' };
const p1input = { padding: '6px 9px', borderRadius: 4, border: '1px solid rgba(0,0,0,0.23)', fontFamily: 'Roboto', fontSize: 13, color: 'rgba(0,0,0,0.87)', background: '#fff' };

function SourceBadge({ src }) {
  const sap = src === 'sap';
  return (
    <span title={sap ? 'Synced from SAP — not editable here' : 'Synced from the master catalog — not editable here'} style={{ display: 'inline-flex', alignItems: 'center', gap: 3, height: 18, padding: '0 6px', borderRadius: 3, fontSize: 10, fontWeight: 600, letterSpacing: '0.03em', whiteSpace: 'nowrap', background: sap ? 'rgba(0,105,92,0.09)' : 'rgba(81,45,168,0.08)', color: sap ? '#00695c' : '#512da8', border: `1px solid ${sap ? 'rgba(0,105,92,0.25)' : 'rgba(81,45,168,0.22)'}` }}>
      <Icon name={sap ? 'cloud_sync' : 'sync_alt'} size={11}/>{sap ? 'From SAP' : 'From master catalog'}
    </span>
  );
}

const P1_STATUS_STYLE = {
  OK:           { label: 'OK',           bg: 'rgba(46,125,50,0.12)',  fg: '#1b5e20' },
  LOW:          { label: 'Low',          bg: 'rgba(237,108,2,0.14)',  fg: '#b45309' },
  OOS:          { label: 'OOS',          bg: 'rgba(211,47,47,0.12)',  fg: '#c62828' },
  OOS_CONFLICT: { label: 'OOS conflict', bg: '#fff', fg: '#c62828', border: '1px dashed #d32f2f' },
};
function StockChip({ s, small }) {
  const st = P1_STATUS_STYLE[s] || P1_STATUS_STYLE.OK;
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, height: small ? 20 : 24, padding: `0 ${small ? 7 : 10}px`, borderRadius: 12, fontSize: small ? 11 : 12, fontWeight: 500, whiteSpace: 'nowrap', background: st.bg, color: st.fg, border: st.border || 'none' }}>{s === 'OOS_CONFLICT' && <Icon name="sync_problem" size={13}/>}{st.label}</span>;
}
function CapChip({ v }) {
  const off = v === 0, full = v === 100;
  return <span style={{ display: 'inline-flex', alignItems: 'center', height: 22, padding: '0 9px', borderRadius: 11, fontSize: 12, fontWeight: 500, whiteSpace: 'nowrap', background: off ? '#d32f2f' : full ? 'rgba(46,125,50,0.12)' : 'rgba(237,108,2,0.14)', color: off ? '#fff' : full ? '#1b5e20' : '#b45309' }}>{off ? 'Offline' : full ? 'Full' : 'Reduced'}</span>;
}
function FactoryTag({ f, dim }) {
  const fc = (FACTORIES.find(x => x.id === f) || {}).color || '#607d8b';
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, height: 22, padding: '0 8px', borderRadius: 4, fontSize: 11, fontWeight: 600, letterSpacing: '0.03em', background: dim ? 'rgba(0,0,0,0.04)' : `${fc}14`, color: dim ? 'rgba(0,0,0,0.38)' : fc, border: `1px solid ${dim ? 'rgba(0,0,0,0.08)' : fc + '40'}` }}><span style={{ width: 6, height: 6, borderRadius: '50%', background: dim ? 'rgba(0,0,0,0.25)' : fc }}></span>{f}</span>;
}
function ActionRequired() {
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 3, fontSize: 11, fontWeight: 500, color: '#c62828', whiteSpace: 'nowrap' }}><Icon name="error" size={14}/>Action required</span>;
}
function ReadOnlyNote({ text }) {
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, fontSize: 12, color: 'rgba(0,0,0,0.55)', background: 'rgba(0,0,0,0.04)', padding: '4px 10px', borderRadius: 4 }}><Icon name="lock" size={14}/>{text || 'Read only for your role'}</span>;
}
function P1Tile({ icon, label, value, sub, color }) {
  const c = color || '#1976d2';
  return (
    <div style={{ ...p1card, padding: 16, display: 'flex', alignItems: 'center', gap: 14, minWidth: 0 }}>
      <div style={{ width: 40, height: 40, borderRadius: 8, background: `${c}14`, color: c, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}><Icon name={icon} size={22}/></div>
      <div style={{ minWidth: 0 }}>
        <div style={{ fontSize: 11, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'rgba(0,0,0,0.6)' }}>{label}</div>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 8, flexWrap: 'wrap' }}><span style={{ fontWeight: 300, fontSize: 28, lineHeight: 1.15 }}>{value}</span>{sub && <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>{sub}</span>}</div>
      </div>
    </div>
  );
}
function P1Tabs({ tabs, value, onChange }) {
  return (
    <div style={{ display: 'flex', borderBottom: '1px solid rgba(0,0,0,0.12)', flexWrap: 'wrap' }}>
      {tabs.map(t => { const a = value === t.id; return (
        <div key={t.id} onClick={() => onChange(t.id)} style={{ padding: '12px 16px', fontSize: 14, cursor: 'pointer', color: a ? '#1976d2' : 'rgba(0,0,0,0.6)', fontWeight: a ? 500 : 400, borderBottom: `2px solid ${a ? '#1976d2' : 'transparent'}`, marginBottom: -1, display: 'inline-flex', alignItems: 'center', gap: 6 }}>
          {t.label}{t.count != null && <span style={{ fontSize: 11, padding: '0 6px', borderRadius: 8, background: 'rgba(0,0,0,0.08)', color: 'rgba(0,0,0,0.6)' }}>{t.count}</span>}
        </div>); })}
    </div>
  );
}
function P1Back({ label, onClick, crumb }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: 'rgba(0,0,0,0.6)' }}>
      <span onClick={onClick} style={{ cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 4 }}><Icon name="arrow_back" size={16}/>{label}</span>
      <Icon name="chevron_right" size={14}/><span style={p1mono}>{crumb}</span>
    </div>
  );
}
function P1Search({ value, onChange, placeholder, width }) {
  return (
    <div style={{ position: 'relative', flex: 1, minWidth: 220, maxWidth: width || 360 }}>
      <span style={{ position: 'absolute', left: 10, top: 8, color: 'rgba(0,0,0,0.54)' }}><Icon name="search" size={20}/></span>
      <input value={value} onChange={e => onChange(e.target.value)} placeholder={placeholder} style={{ width: '100%', boxSizing: 'border-box', padding: '8px 12px 8px 36px', borderRadius: 4, border: '1px solid rgba(0,0,0,0.23)', fontFamily: 'Roboto', fontSize: 14 }}/>
    </div>
  );
}
function SyncStamp({ label, when }) {
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'rgba(0,0,0,0.6)' }}><Icon name="schedule" size={15}/>{label}: <b style={{ fontWeight: 500, color: 'rgba(0,0,0,0.8)' }}>{when}</b></span>;
}
function HistoryTable({ rows }) {
  return (
    <div style={{ ...p1card, overflow: 'hidden' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead><tr><th style={p1th}>When</th><th style={p1th}>Who</th><th style={p1th}>Field</th><th style={p1th}>Old value</th><th style={p1th}>New value</th></tr></thead>
        <tbody>
          {rows.map((h, i) => { const sync = /sync/i.test(h.who); return (
            <tr key={i}>
              <td style={{ ...p1td, ...p1mono, color: 'rgba(0,0,0,0.6)', whiteSpace: 'nowrap' }}>{h.when}</td>
              <td style={p1td}><span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>{sync ? <Icon name="cloud_sync" size={16} sx={{ color: '#00695c' }}/> : <Icon name="person" size={16} sx={{ color: 'rgba(0,0,0,0.45)' }}/>}{h.who}</span></td>
              <td style={p1td}>{h.field}</td>
              <td style={{ ...p1td, color: 'rgba(0,0,0,0.5)', textDecoration: 'line-through' }}>{h.old}</td>
              <td style={{ ...p1td, fontWeight: 500 }}>{h.nw}</td>
            </tr>); })}
          {!rows.length && <tr><td colSpan="5" style={{ ...p1td, textAlign: 'center', color: 'rgba(0,0,0,0.5)', padding: 24 }}>No changes recorded yet.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
function P1Field({ label, value, mono, badge, hint, wide }) {
  return (
    <div style={{ gridColumn: wide ? '1 / -1' : 'auto', padding: '11px 14px', borderRadius: 6, background: '#f5f7fa', border: '1px solid rgba(0,0,0,0.06)', minWidth: 0 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 4 }}><span style={{ fontSize: 11, color: 'rgba(0,0,0,0.6)', textTransform: 'uppercase', letterSpacing: '0.06em' }}>{label}</span>{badge && <SourceBadge src={badge}/>}</div>
      <div style={{ fontSize: 15, color: 'rgba(0,0,0,0.87)', fontWeight: mono ? 400 : 500, wordBreak: 'break-word', ...(mono ? p1mono : {}) }}>{value}</div>
      {hint && <div style={{ fontSize: 11, color: '#b45309', marginTop: 3, display: 'inline-flex', alignItems: 'center', gap: 4 }}><Icon name="help" size={13}/>{hint}</div>}
    </div>
  );
}
function P1Card({ title, sub, right, children, pad }) {
  return (
    <div style={{ ...p1card, overflow: 'hidden' }}>
      {title && <div style={{ padding: '14px 18px', borderBottom: '1px solid rgba(0,0,0,0.08)', display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <div style={{ flex: 1, minWidth: 200 }}><div style={{ fontSize: 16, fontWeight: 500 }}>{title}</div>{sub && <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)', marginTop: 2 }}>{sub}</div>}</div>{right}
      </div>}
      <div style={{ padding: pad === 0 ? 0 : (pad || 18) }}>{children}</div>
    </div>
  );
}

/* ---------- Column-header filtering + sorting ---------- */
const NUM_OPS = [['eq', '='], ['ne', '≠'], ['lt', '<'], ['le', '≤'], ['gt', '>'], ['ge', '≥'], ['between', 'Between']];
function _cfPass(def, f, row) {
  const v = def.get(row);
  if (def.type === 'enum') { if (!f || !f.length) return true; const arr = Array.isArray(v) ? v : [v]; return arr.some(x => f.includes(String(x))); }
  if (def.type === 'number') {
    if (!f || f.a === '' || f.a == null) return true; const a = parseFloat(f.a), b = parseFloat(f.b); const n = typeof v === 'number' ? v : parseFloat(v);
    if (isNaN(n)) return false;
    switch (f.op) { case 'eq': return n === a; case 'ne': return n !== a; case 'lt': return n < a; case 'le': return n <= a; case 'gt': return n > a; case 'ge': return n >= a; case 'between': return isNaN(b) ? n >= a : n >= a && n <= b; default: return true; }
  }
  if (!f) return true; return String(v ?? '').toLowerCase().includes(String(f).toLowerCase());
}
function _cfActive(def, f) { if (def.type === 'enum') return !!(f && f.length); if (def.type === 'number') return !!(f && f.a !== '' && f.a != null); return !!f; }
function useColFilters(allRows, defs) {
  const [filters, setFilters] = React.useState({});
  const [sort, setSort] = React.useState(null);
  let rows = allRows.filter(r => Object.keys(filters).every(k => !defs[k] || _cfPass(defs[k], filters[k], r)));
  if (sort && defs[sort.k]) {
    const g = defs[sort.k].get; const dir = sort.dir === 'asc' ? 1 : -1;
    rows = rows.slice().sort((x, y) => { let a = g(x), b = g(y); if (Array.isArray(a)) a = a.join(','); if (Array.isArray(b)) b = b.join(','); if (a == null) return 1; if (b == null) return -1; return (typeof a === 'number' && typeof b === 'number' ? a - b : String(a).localeCompare(String(b), undefined, { numeric: true })) * dir; });
  }
  const setFilter = (k, v) => setFilters(prev => { const n = { ...prev }; if (v == null || (Array.isArray(v) && !v.length) || v === '') delete n[k]; else n[k] = v; return n; });
  const options = (k) => { const d = defs[k]; if (d.options) return d.options; const set = new Set(); allRows.forEach(r => { const v = d.get(r); (Array.isArray(v) ? v : [v]).forEach(x => x != null && x !== '' && set.add(String(x))); }); return [...set].sort((a, b) => a.localeCompare(b, undefined, { numeric: true })); };
  const activeCount = Object.keys(filters).filter(k => defs[k] && _cfActive(defs[k], filters[k])).length;
  return { rows, filters, setFilter, sort, setSort, defs, options, activeCount, clearAll: () => setFilters({}) };
}
function FilterTh({ k, label, cf, style, align }) {
  const def = cf.defs[k];
  const [open, setOpen] = React.useState(null);
  const [q, setQ] = React.useState('');
  const ref = React.useRef(null);
  if (!def) return <th style={{ ...p1th, ...style }}>{label}</th>;
  const f = cf.filters[k]; const active = _cfActive(def, f); const sorted = cf.sort && cf.sort.k === k ? cf.sort.dir : null;
  const toggle = () => { if (open) return setOpen(null); const r = ref.current.getBoundingClientRect(); setOpen({ top: r.bottom + 6, left: Math.min(r.left - 8, window.innerWidth - 270) }); setQ(''); };
  const opts = def.type === 'enum' ? cf.options(k) : [];
  const nf = f || { op: 'ge', a: '', b: '' };
  return (
    <th style={{ ...p1th, ...style, textAlign: align || 'left' }}>
      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 3 }}>
        <span onClick={toggle} style={{ cursor: 'pointer', color: active ? '#1976d2' : undefined }}>{label}</span>
        <span ref={ref} onClick={toggle} title="Filter and sort" style={{ display: 'inline-flex', cursor: 'pointer', color: active || sorted ? '#1976d2' : 'rgba(0,0,0,0.4)' }}>
          <Icon name={sorted === 'asc' ? 'arrow_upward' : sorted === 'desc' ? 'arrow_downward' : 'unfold_more'} size={15}/>
        </span>
        {active && <span onClick={() => cf.setFilter(k, null)} title="Clear filter" style={{ display: 'inline-flex', cursor: 'pointer', color: 'rgba(0,0,0,0.5)' }}><Icon name="close" size={13}/></span>}
      </span>
      {open && <>
        <div onClick={() => setOpen(null)} style={{ position: 'fixed', inset: 0, zIndex: 40 }}></div>
        <div style={{ position: 'fixed', top: open.top, left: open.left, zIndex: 41, width: 250, background: '#fff', borderRadius: 8, boxShadow: '0 8px 28px rgba(0,0,0,0.18)', padding: 12, textTransform: 'none', letterSpacing: 0, fontWeight: 400, color: 'rgba(0,0,0,0.87)', fontSize: 13 }}>
          <div style={{ fontWeight: 500, fontSize: 14, marginBottom: 8 }}>Filter by {label}</div>
          <div style={{ display: 'flex', gap: 6, marginBottom: 10 }}>
            {[['asc', 'arrow_upward', 'Ascending'], ['desc', 'arrow_downward', 'Descending']].map(([d, ic, l]) => (
              <button key={d} onClick={() => cf.setSort(sorted === d ? null : { k, dir: d })} style={{ flex: 1, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 4, padding: '5px 6px', borderRadius: 4, cursor: 'pointer', fontFamily: 'Roboto', fontSize: 12, border: `1px solid ${sorted === d ? '#1976d2' : 'rgba(0,0,0,0.2)'}`, background: sorted === d ? 'rgba(25,118,210,0.08)' : '#fff', color: sorted === d ? '#1976d2' : 'rgba(0,0,0,0.7)' }}><Icon name={ic} size={14}/>{l}</button>))}
          </div>
          {def.type === 'enum' && <>
            {opts.length > 6 && <input autoFocus value={q} onChange={e => setQ(e.target.value)} placeholder={`Search ${label.toLowerCase()}…`} style={{ ...p1input, width: '100%', boxSizing: 'border-box', marginBottom: 6 }}/>}
            <div style={{ maxHeight: 240, overflowY: 'auto' }}>
              {opts.filter(o => !q || o.toLowerCase().includes(q.toLowerCase())).map(o => { const on = (f || []).includes(o); return (
                <label key={o} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '6px 4px', cursor: 'pointer', borderRadius: 4 }}>
                  <input type="checkbox" checked={on} onChange={() => cf.setFilter(k, on ? (f || []).filter(x => x !== o) : [...(f || []), o])} style={{ width: 16, height: 16, margin: 0 }}/>{def.label ? def.label(o) : o}
                </label>); })}
            </div>
          </>}
          {def.type === 'number' && <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            <select value={nf.op} onChange={e => cf.setFilter(k, { ...nf, op: e.target.value })} style={{ ...p1select, width: '100%' }}>{NUM_OPS.map(([v, l]) => <option key={v} value={v}>{l}</option>)}</select>
            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
              <input autoFocus type="number" value={nf.a} onChange={e => cf.setFilter(k, { ...nf, a: e.target.value })} placeholder="Value" style={{ ...p1input, flex: 1, minWidth: 0 }}/>
              {nf.op === 'between' && <><span>–</span><input type="number" value={nf.b} onChange={e => cf.setFilter(k, { ...nf, b: e.target.value })} placeholder="Max" style={{ ...p1input, flex: 1, minWidth: 0 }}/></>}
            </div>
          </div>}
          {def.type === 'text' && <input autoFocus value={f || ''} onChange={e => cf.setFilter(k, e.target.value)} placeholder="Contains…" style={{ ...p1input, width: '100%', boxSizing: 'border-box' }}/>}
          {active && <div style={{ borderTop: '1px solid rgba(0,0,0,0.08)', marginTop: 10, paddingTop: 8 }}><span onClick={() => cf.setFilter(k, null)} style={{ fontSize: 12, color: '#1976d2', cursor: 'pointer' }}>Clear filter</span></div>}
        </div>
      </>}
    </th>
  );
}
function FilterSummary({ cf, total, noun }) {
  return (
    <div style={{ padding: '8px 16px', fontSize: 13, color: 'rgba(0,0,0,0.6)', display: 'flex', justifyContent: 'flex-end', gap: 14, alignItems: 'center' }}>
      {cf.activeCount > 0 && <span onClick={cf.clearAll} style={{ color: '#1976d2', cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 4 }}><Icon name="filter_alt_off" size={15}/>Clear {cf.activeCount} filter{cf.activeCount > 1 ? 's' : ''}</span>}
      <span>{cf.rows.length} of {total} {noun}</span>
    </div>
  );
}

Object.assign(window, { useColFilters, FilterTh, FilterSummary });
Object.assign(window, { P1Context, useP1Ctx, p1th, p1td, p1mono, p1card, p1select, p1input, SourceBadge, StockChip, CapChip, FactoryTag, ActionRequired, ReadOnlyNote, P1Tile, P1Tabs, P1Back, P1Search, SyncStamp, HistoryTable, P1Field, P1Card, P1_STATUS_STYLE });

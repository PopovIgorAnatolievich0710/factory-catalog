/* global p1Today, P1_UOMS, P1_UOM_SHORT, p1Qty, React, Icon, IconButton, Button, Chip, Switch, P1_F, P1_STORE, P1_CAN, P1_NOW, P1_FX, useP1Store, useP1Ctx, p1Bump, p1Log, p1LaborUSD, p1SkusUsingProc,
   p1th, p1td, p1mono, p1card, p1select, p1input, CapChip, FactoryTag, ReadOnlyNote, P1Tabs, P1Back, P1Search, HistoryTable, P1Field, P1Card, FACTORIES */
const { useState: useStateP1P } = React;

const runningF = (p) => P1_F.filter(f => p.factories[f].runs);
const lowestCap = (p) => { const r = runningF(p); return r.length ? Math.min(...r.map(f => p.factories[f].capacity)) : null; };
const avgLaborUSD = (p) => { const r = runningF(p); return r.length ? r.reduce((n, f) => n + p1LaborUSD(p.factories[f]), 0) / r.length : null; };
const machineBase = (p) => { const r = runningF(p).map(f => p.factories[f].machine_type).filter(Boolean); const uniq = [...new Set(r)]; return uniq; };
function TypeChip({ t }) {
  const m = t === 'Machine';
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, height: 22, padding: '0 9px', borderRadius: 11, fontSize: 12, fontWeight: 500, background: m ? 'rgba(25,118,210,0.1)' : 'rgba(109,76,65,0.1)', color: m ? '#1565c0' : '#5d4037' }}><Icon name={m ? 'precision_manufacturing' : 'back_hand'} size={14}/>{t}</span>;
}
function capColor(v) { return v === 0 ? '#d32f2f' : v < 100 ? '#ed6c02' : '#2e7d32'; }

function P1ProcessesList({ onOpen }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const [q, setQ] = useStateP1P('');
  const [creating, setCreating] = useStateP1P(false); const [nn, setNn] = useStateP1P(''); const [nt, setNt] = useStateP1P('Work');
  const base = Object.values(S.procs).filter(p => !q || p.name.toLowerCase().includes(q.toLowerCase()) || p.id.toLowerCase().includes(q.toLowerCase()));
  const cf = useColFilters(base, {
    id: { type: 'text', get: p => p.id }, name: { type: 'text', get: p => p.name },
    type: { type: 'enum', get: p => p.type, options: ['Machine', 'Work'] },
    machine: { type: 'enum', get: p => p.type === 'Machine' ? (machineBase(p)[0] || '—').split(' · ')[0] : 'n/a' },
    fac: { type: 'enum', get: p => runningF(p), options: P1_F },
    low: { type: 'number', get: p => lowestCap(p) }, avg: { type: 'number', get: p => { const v = avgLaborUSD(p); return v == null ? null : +v.toFixed(2); } },
    skus: { type: 'number', get: p => p1SkusUsingProc(p.id).length },
  });
  const rows = cf.rows;
  const create = () => {
    if (!nn.trim()) return;
    const id = 'PRC-N' + (Object.keys(S.procs).length + 1);
    const factories = {}; P1_F.forEach(f => { factories[f] = { runs: false, machine_type: nt === 'Machine' ? '' : null, labor_cost: 0, currency: { KG: 'ILS', NZ: 'ILS', HU: 'HUF', TH: 'THB' }[f], capacity: 100, reason: '', recovery: '', changed_by: ctx.user.name, changed_at: P1_NOW.split(' · ')[0] }; });
    S.procs[id] = { id, name: nn.trim(), type: nt, description: '', status: 'Active', factories, history: [{ who: ctx.user.name, when: P1_NOW, field: 'Process', old: '—', nw: 'Created' }] };
    p1Log('Process', nn.trim(), ctx.user.name, 'Process', '—', 'Created');
    setCreating(false); setNn(''); p1Bump(); ctx.toast(`Process “${nn.trim()}” created — set the factories it runs in`); onOpen(id);
  };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={p1card}>
        <div style={{ padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10, borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
          <P1Search value={q} onChange={setQ} placeholder="Search process ID or name…"/>
          <div style={{ flex: 1 }}></div>
          {P1_CAN.newProcess(ctx) && <Button variant="contained" startIcon="add" onClick={() => setCreating(true)}>New process</Button>}
        </div>
        {creating && <div style={{ padding: '12px 16px', display: 'flex', gap: 10, alignItems: 'center', background: 'rgba(25,118,210,0.04)', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
          <input autoFocus value={nn} onChange={e => setNn(e.target.value)} placeholder="Process name" style={{ ...p1input, minWidth: 260 }}/>
          <select value={nt} onChange={e => setNt(e.target.value)} style={p1select}><option>Work</option><option>Machine</option></select>
          <Button variant="contained" size="small" disabled={!nn.trim()} onClick={create}>Create</Button><Button variant="text" size="small" onClick={() => setCreating(false)}>Cancel</Button>
        </div>}
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead><tr><FilterTh k="id" label="Process ID" cf={cf}/><FilterTh k="name" label="Process" cf={cf}/><FilterTh k="type" label="Type" cf={cf}/><FilterTh k="machine" label="Machine type" cf={cf}/><FilterTh k="fac" label="Factories running it" cf={cf}/><FilterTh k="low" label="Lowest capacity today" cf={cf}/><FilterTh k="avg" label="Avg labor cost ($)" cf={cf}/><FilterTh k="skus" label="SKUs using" cf={cf}/></tr></thead>
            <tbody>{rows.map(p => { const low = lowestCap(p), avg = avgLaborUSD(p), mt = machineBase(p); return (
              <tr key={p.id} onClick={() => onOpen(p.id)} style={{ cursor: 'pointer' }} onMouseEnter={e => e.currentTarget.style.background = 'rgba(0,0,0,0.03)'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                <td style={{ ...p1td, ...p1mono, color: '#1976d2', fontWeight: 500, whiteSpace: 'nowrap' }}>{p.id}</td>
                <td style={{ ...p1td, fontWeight: 500 }}>{p.name}</td>
                <td style={p1td}><TypeChip t={p.type}/></td>
                <td style={{ ...p1td, fontSize: 13, color: 'rgba(0,0,0,0.7)' }}>{p.type === 'Machine' ? (mt.length ? <>{mt[0].split(' · ')[0]}{mt.length > 1 && <span style={{ color: 'rgba(0,0,0,0.45)' }}> · {mt.length} models</span>}</> : '—') : <span style={{ color: 'rgba(0,0,0,0.35)' }}>n/a</span>}</td>
                <td style={p1td}><div style={{ display: 'flex', gap: 4 }}>{P1_F.map(f => <FactoryTag key={f} f={f} dim={!p.factories[f].runs}/>)}</div></td>
                <td style={p1td}>{low == null ? <span style={{ color: 'rgba(0,0,0,0.4)' }}>—</span> : <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}><b style={{ fontWeight: 500, color: capColor(low) }}>{low}%</b><CapChip v={low}/></span>}</td>
                <td style={{ ...p1td, ...p1mono }}>{avg == null ? '—' : `$${avg.toFixed(2)}`}</td>
                <td style={p1td}>{p1SkusUsingProc(p.id).length}</td>
              </tr>); })}</tbody>
          </table>
        </div>
        <FilterSummary cf={cf} total={Object.keys(S.procs).length} noun="processes"/>
      </div>
    </div>
  );
}

function P1ProcessDetail({ id, onBack, onOpenSku }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const p = S.procs[id] || Object.values(S.procs)[0]; const [tab, setTab] = useStateP1P('spec');
  const isAdmin = P1_CAN.editProcessFactories(ctx);
  const skus = p1SkusUsingProc(p.id);
  const skuCf = useColFilters(skus.map(sk => ({ s: sk, m: sk.processes.find(x => x.procId === p.id) })), { name: { type: 'text', get: r => r.s.name }, site: { type: 'text', get: r => r.s.site_sku }, qty: { type: 'number', get: r => r.m.qty } });
  const log = (field, old, nw) => { p.history.unshift({ who: ctx.user.name, when: P1_NOW, field, old: String(old ?? '—'), nw: String(nw) }); p1Log('Process', p.name, ctx.user.name, field, String(old ?? '—'), String(nw)); };
  const setSpec = (key, val, label) => { const old = p[key]; if (old === val) return; p[key] = val; log(label, old, val); p1Bump(); };
  const setF = (f, key, val, label) => { const old = p.factories[f][key]; if (old === val) return; p.factories[f][key] = val; log(`${label} · ${f}`, old, val); p1Bump(); };
  const unit = P1_UOM_SHORT[p.uom] || p.uom;
  const rowTh = { textAlign: 'left', width: 200, padding: '12px 18px', fontSize: 13, fontWeight: 500, color: 'rgba(0,0,0,0.6)', borderBottom: '1px solid rgba(0,0,0,0.06)', verticalAlign: 'top' };
  const rowTd = { padding: '10px 18px', fontSize: 14, color: 'rgba(0,0,0,0.87)', borderBottom: '1px solid rgba(0,0,0,0.06)' };
  const specRows = [
    ['Name', isAdmin ? <input defaultValue={p.name} onBlur={e => setSpec('name', e.target.value.trim() || p.name, 'Name')} style={{ ...p1input, width: '100%', maxWidth: 380 }}/> : p.name],
    ['Type', isAdmin ? <select value={p.type} onChange={e => setSpec('type', e.target.value, 'Type')} style={p1select}><option>Machine</option><option>Work</option></select> : <TypeChip t={p.type}/>],
    ['Measured in', isAdmin ? <select value={p.uom} onChange={e => setSpec('uom', e.target.value, 'Measured in')} style={p1select}>{P1_UOMS.map(u => <option key={u}>{u}</option>)}</select> : p.uom],
    ['Status', isAdmin ? <select value={p.status} onChange={e => setSpec('status', e.target.value, 'Status')} style={p1select}><option>Active</option><option>Inactive</option></select> : p.status],
    ['Description', isAdmin ? <textarea defaultValue={p.description} onBlur={e => setSpec('description', e.target.value, 'Description')} rows="3" style={{ ...p1input, width: '100%', maxWidth: 520, resize: 'vertical', fontFamily: 'Roboto' }}></textarea> : (p.description || '—')],
  ];
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <P1Back label="Processes" onClick={onBack} crumb={p.id}/>
      <div style={{ ...p1card, padding: 22, display: 'flex', gap: 16, alignItems: 'center', flexWrap: 'wrap' }}>
        <span style={{ width: 52, height: 52, borderRadius: 10, background: 'rgba(25,118,210,0.08)', color: '#1976d2', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}><Icon name={p.type === 'Machine' ? 'precision_manufacturing' : 'back_hand'} size={28}/></span>
        <div style={{ flex: 1 }}><div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 4 }}><span style={{ ...p1mono, color: '#1976d2', fontWeight: 500 }}>{p.id}</span><TypeChip t={p.type}/><Chip label={p.status} color={p.status === 'Active' ? 'success' : 'default'} size="small"/><span style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>Measured in {p.uom}</span></div><h2 style={{ margin: 0, fontSize: 22, fontWeight: 400 }}>{p.name}</h2></div>
        <div style={{ display: 'flex', gap: 10 }}>{P1_F.map(f => <span key={f} style={{ display: 'inline-flex', flexDirection: 'column', alignItems: 'center', gap: 3 }}><FactoryTag f={f} dim={!p.factories[f].runs}/><span style={{ fontSize: 12, fontWeight: 500, color: p.factories[f].runs ? capColor(p.factories[f].capacity) : 'rgba(0,0,0,0.3)' }}>{p.factories[f].runs ? p.factories[f].capacity + '%' : '—'}</span></span>)}</div>
      </div>
      <P1Tabs value={tab} onChange={setTab} tabs={[{ id: 'spec', label: 'Specifications' }, { id: 'fac', label: 'Factories' }, { id: 'skus', label: 'SKUs using this', count: skus.length }, { id: 'history', label: 'Audit' }]}/>
      {tab === 'spec' && <P1Card title="Specifications" right={!isAdmin && <ReadOnlyNote text="Factory catalog admin edits"/>} pad={0}>
        <table style={{ width: '100%', maxWidth: 820, borderCollapse: 'collapse' }}>
          <tbody>{specRows.map(([l, v], i) => <tr key={l} style={{ background: i % 2 ? '#fafbfc' : '#fff' }}><th style={rowTh}>{l}</th><td style={rowTd}>{v}</td></tr>)}</tbody>
        </table>
      </P1Card>}
      {tab === 'fac' && <P1Card title="Factories" right={<ReadOnlyNote text="Factory manager edits"/>} pad={0}>
        <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', minWidth: 760, borderCollapse: 'collapse', tableLayout: 'fixed' }}>
          <colgroup><col style={{ width: 90 }}/><col style={{ width: 110 }}/>{p.type === 'Machine' && <col style={{ width: 200 }}/>}<col style={{ width: 210 }}/><col style={{ width: 170 }}/><col/></colgroup>
          <thead><tr><th style={p1th}>Factory</th><th style={p1th}>Runs here</th>{p.type === 'Machine' && <th style={p1th}>Machine type</th>}<th style={p1th}>Labor cost per {unit}</th><th style={p1th} title="What 100% capacity means">Max per day (100%)</th><th style={p1th}>Capacity · today</th></tr></thead>
          <tbody>{P1_F.map(f => { const x = p.factories[f]; const can = ctx.role === 'factory_manager' && ctx.factory === f; const cell = { ...p1td, height: 56 }; return (
            <tr key={f} style={{ background: can ? 'rgba(25,118,210,0.035)' : 'transparent' }}>
              <td style={cell}><FactoryTag f={f} dim={!x.runs}/></td>
              <td style={cell}><AltCheckP label={x.runs ? 'Yes' : 'No'} checked={x.runs} active={can} onChange={v => setF(f, 'runs', v, 'Runs here')}/></td>
              {p.type === 'Machine' && <td style={cell}>{can && x.runs ? <input defaultValue={x.machine_type || ''} onBlur={e => setF(f, 'machine_type', e.target.value, 'Machine type')} style={{ ...p1input, width: '100%', boxSizing: 'border-box' }}/> : <span style={{ color: x.runs ? 'inherit' : 'rgba(0,0,0,0.35)' }}>{x.runs ? x.machine_type || '—' : '—'}</span>}</td>}
              <td style={cell}>{can && x.runs ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><input type="number" defaultValue={x.labor_cost} onBlur={e => setF(f, 'labor_cost', parseFloat(e.target.value) || 0, 'Labor cost')} style={{ ...p1input, width: 110 }}/><span style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>{x.currency}</span></span> : x.runs ? <span style={p1mono}>{x.labor_cost.toLocaleString()} {x.currency} <span style={{ color: 'rgba(0,0,0,0.45)' }}>≈ ${p1LaborUSD(x).toFixed(2)}</span></span> : <span style={{ color: 'rgba(0,0,0,0.35)' }}>—</span>}</td>
              <td style={cell}>{can && x.runs ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><input type="number" min="0" defaultValue={x.max_per_day} onBlur={e => setF(f, 'max_per_day', parseInt(e.target.value, 10) || 0, 'Max per day')} style={{ ...p1input, width: 90 }}/><span style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>items/day</span></span> : x.runs ? <span style={p1mono}>{x.max_per_day.toLocaleString()} <span style={{ color: 'rgba(0,0,0,0.45)', fontFamily: 'Roboto' }}>items/day</span></span> : <span style={{ color: 'rgba(0,0,0,0.35)' }}>—</span>}</td>
              <td style={cell}>{x.runs ? (() => { const t = p1Today(x); return <div><span style={{ display: 'inline-flex', gap: 8, alignItems: 'center' }}><b style={{ fontWeight: 500, color: capColor(x.capacity) }}>{x.capacity}%</b><CapChip v={x.capacity}/></span><div style={{ fontSize: 11.5, color: 'rgba(0,0,0,0.55)', marginTop: 2 }}>{t.used} of {t.available} used today · <b style={{ fontWeight: 500, color: capColor(t.remainingPct === 0 ? 0 : t.remainingPct < 100 ? 50 : 100) }}>{t.remainingPct}% left</b></div></div>; })() : <span style={{ color: 'rgba(0,0,0,0.35)' }}>—</span>}</td>
            </tr>); })}</tbody>
        </table>
        </div>
      </P1Card>}
      {tab === 'skus' && <P1Card title="SKUs using this process" right={<ReadOnlyNote text="Read only · Source: Info file / Supplier"/>} pad={0}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><FilterTh k="name" label="Product" cf={skuCf}/><FilterTh k="site" label="Site SKU" cf={skuCf}/><FilterTh k="qty" label={`Quantity required (${unit})`} cf={skuCf}/></tr></thead>
          <tbody>{skuCf.rows.map(({ s, m }) => { return <tr key={s.id} onClick={() => onOpenSku(s.id)} style={{ cursor: 'pointer' }}><td style={{ ...p1td, fontWeight: 500 }}>{s.name}</td><td style={{ ...p1td, ...p1mono }}>{s.site_sku}</td><td style={{ ...p1td, ...p1mono, fontWeight: 500 }}>{p1Qty(m.qty, p.uom)}</td></tr>; })}
            {!skuCf.rows.length && <tr><td colSpan="3" style={{ ...p1td, textAlign: 'center', padding: 24, color: 'rgba(0,0,0,0.5)' }}>No SKUs map this process.</td></tr>}</tbody>
        </table>
      </P1Card>}
      {tab === 'history' && <HistoryTable rows={p.history}/>}
    </div>
  );
}

function AltCheckP({ label, checked, active, onChange }) {
  const box = checked ? (active ? '#1976d2' : 'rgba(25,118,210,0.4)') : 'transparent';
  const border = checked ? box : active ? 'rgba(0,0,0,0.6)' : 'rgba(0,0,0,0.22)';
  return (
    <label onClick={() => active && onChange(!checked)} style={{ display: 'inline-flex', alignItems: 'center', gap: 7, cursor: active ? 'pointer' : 'not-allowed', userSelect: 'none' }}>
      <span style={{ width: 16, height: 16, borderRadius: 3, border: `2px solid ${border}`, background: box, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color: '#fff', boxSizing: 'border-box' }}>{checked && <Icon name="check" size={12}/>}</span>
      <span style={{ fontSize: 13, color: active ? 'rgba(0,0,0,0.8)' : 'rgba(0,0,0,0.45)' }}>{label}</span>
    </label>
  );
}

/* ================= Process capacity ================= */
function P1ProcessCapacity() {
  const S = useP1Store(); const ctx = useP1Ctx();
  const isFM = ctx.role === 'factory_manager', isOps = P1_CAN.seeAllFactories(ctx);
  const [view, setView] = useStateP1P(isOps ? 'all' : 'one');
  const [fac, setFac] = useStateP1P(isFM ? ctx.factory : 'NZ');
  const f = isFM ? ctx.factory : fac;
  const showGrid = !isFM && (!isOps || view === 'all');
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        {isOps && <div style={{ display: 'inline-flex', padding: 3, background: '#eceff1', borderRadius: 8 }}>
          {[['all', 'All factories'], ['one', 'By factory']].map(([k, l]) => <button key={k} onClick={() => setView(k)} style={{ border: 'none', cursor: 'pointer', padding: '6px 14px', borderRadius: 6, fontFamily: 'Roboto', fontSize: 13, fontWeight: 500, background: view === k ? '#fff' : 'transparent', color: view === k ? '#1976d2' : 'rgba(0,0,0,0.6)', boxShadow: view === k ? '0 1px 3px rgba(0,0,0,0.15)' : 'none' }}>{l}</button>)}
        </div>}
        {isOps && view === 'one' && <select value={fac} onChange={e => setFac(e.target.value)} style={p1select}>{P1_F.map(x => <option key={x} value={x}>Factory {x}</option>)}</select>}
        {isFM && <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8, fontSize: 14 }}>Your factory <FactoryTag f={f}/></span>}
        <div style={{ flex: 1 }}></div>
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'rgba(0,0,0,0.6)' }}><Icon name="info" size={15}/>Capacity changes only when you change it.</span>
      </div>
      {showGrid ? <P1CapacityGrid/> : <P1CapacityList f={f} editable={P1_CAN.editCapacity(ctx, f)}/>}
    </div>
  );
}

function P1CapacityList({ f, editable }) {
  const S = useP1Store(); const ctx = useP1Ctx();
  const procs = Object.values(S.procs).filter(p => p.factories[f].runs);
  const setCap = (p, v) => {
    const x = p.factories[f]; const old = x.capacity; if (old === v) return;
    x.capacity = v; x.changed_by = ctx.user.name; x.changed_at = P1_NOW.split(' · ')[0];
    if (v === 100) { x.reason = ''; x.recovery = ''; }
    p.history.unshift({ who: ctx.user.name, when: P1_NOW, field: `Capacity · ${f}`, old: old + '%', nw: v + '%' });
    p1Log('Process', p.name, ctx.user.name, `Capacity · ${f}`, old + '%', v + '%'); p1Bump();
  };
  const counts = { off: procs.filter(p => p.factories[f].capacity === 0).length, red: procs.filter(p => p.factories[f].capacity > 0 && p.factories[f].capacity < 100).length };
  return (
    <div style={p1card}>
      <div style={{ padding: '12px 16px', display: 'flex', gap: 14, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
        <b style={{ fontWeight: 500 }}>{procs.length} processes run at {f}</b>
        <span style={{ fontSize: 13, color: '#c62828' }}>{counts.off} offline</span><span style={{ fontSize: 13, color: '#b45309' }}>{counts.red} reduced</span>
        <div style={{ flex: 1 }}></div>{!editable && <ReadOnlyNote text={`Only the ${f} factory manager edits`}/>}
      </div>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead><tr><th style={p1th}>Process</th><th style={p1th}>Type</th><th style={{ ...p1th, minWidth: 300 }}>Capacity</th><th style={p1th}>Status</th><th style={{ ...p1th, minWidth: 260 }}>Reason · expected recovery</th><th style={p1th}>Last changed</th></tr></thead>
        <tbody>{procs.map(p => { const x = p.factories[f]; return (
          <tr key={p.id} style={{ background: x.capacity === 0 ? 'rgba(211,47,47,0.035)' : 'transparent' }}>
            <td style={{ ...p1td, fontWeight: 500 }}>{p.name}</td>
            <td style={p1td}><TypeChip t={p.type}/></td>
            <td style={p1td}><CapSlider value={x.capacity} disabled={!editable} onChange={v => setCap(p, v)}/><TodayLine x={x}/></td>
            <td style={p1td}><CapChip v={x.capacity}/></td>
            <td style={p1td}>{x.capacity < 100 ? (editable ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
                <input value={x.reason} onChange={e => { x.reason = e.target.value; p1Bump(); }} placeholder="Reason (optional)" style={{ ...p1input, width: '100%' }}/>
                <input value={x.recovery} onChange={e => { x.recovery = e.target.value; p1Bump(); }} placeholder="Expected recovery (optional)" style={{ ...p1input, width: '100%' }}/>
                <span style={{ fontSize: 11, color: 'rgba(0,0,0,0.5)' }}>For information only — capacity changes only when you change it.</span>
              </div>) : <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.7)' }}>{x.reason || <span style={{ color: 'rgba(0,0,0,0.4)' }}>No reason given</span>}{x.recovery && <div style={{ color: 'rgba(0,0,0,0.5)' }}>Expected recovery: {x.recovery}</div>}</div>)
              : <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.35)' }}>—</span>}</td>
            <td style={{ ...p1td, fontSize: 12, color: 'rgba(0,0,0,0.6)', whiteSpace: 'nowrap' }}>Last changed by {x.changed_by}<br/>on {x.changed_at}</td>
          </tr>); })}</tbody>
      </table>
    </div>
  );
}

function TodayLine({ x }) {
  const t = p1Today(x); const pctUsed = t.max ? t.used / t.max * 100 : 0; const pctAvail = t.max ? t.available / t.max * 100 : 0;
  return (
    <div style={{ marginTop: 6, maxWidth: 360 }}>
      <div style={{ position: 'relative', height: 6, borderRadius: 3, background: 'rgba(0,0,0,0.06)', overflow: 'hidden' }} title={`100% = ${t.max} items/day`}>
        <div style={{ position: 'absolute', left: 0, top: 0, bottom: 0, width: pctAvail + '%', background: 'rgba(46,125,50,0.22)' }}></div>
        <div style={{ position: 'absolute', left: 0, top: 0, bottom: 0, width: pctUsed + '%', background: 'rgba(0,0,0,0.45)' }}></div>
      </div>
      <div style={{ fontSize: 11.5, color: 'rgba(0,0,0,0.6)', marginTop: 3 }}>Used <b style={{ fontWeight: 500 }}>{t.used}</b> of {t.available} today (max {t.max}/day) · <b style={{ fontWeight: 500, color: t.remainingPct === 0 ? '#c62828' : 'rgba(0,0,0,0.8)' }}>{t.remainingPct}% left</b></div>
    </div>
  );
}

function CapSlider({ value, onChange, disabled }) {
  const col = capColor(value);
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
      <div style={{ flex: 1, minWidth: 200 }}>
        <input type="range" min="0" max="100" step="10" value={value} disabled={disabled} onChange={e => onChange(parseInt(e.target.value, 10))} style={{ width: '100%', accentColor: col, cursor: disabled ? 'not-allowed' : 'pointer' }}/>
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0 2px' }}>{Array.from({ length: 11 }, (_, i) => <span key={i} style={{ width: 1, height: 5, background: i * 10 === value ? col : 'rgba(0,0,0,0.25)' }}></span>)}</div>
      </div>
      <b style={{ fontWeight: 500, width: 44, textAlign: 'right', color: col }}>{value}%</b>
    </div>
  );
}

function P1CapacityGrid() {
  const S = useP1Store();
  const procs = Object.values(S.procs);
  const below = procs.reduce((n, p) => n + P1_F.filter(f => p.factories[f].runs && p.factories[f].capacity < 100).length, 0);
  return (
    <div style={p1card}>
      <div style={{ padding: '12px 16px', display: 'flex', gap: 16, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap', fontSize: 13 }}>
        <b style={{ fontWeight: 500, fontSize: 14 }}>All factories · current capacity</b>
        <span style={{ color: 'rgba(0,0,0,0.6)' }}>{below} process–factory pairs below 100%</span>
        <div style={{ flex: 1 }}></div>
        {[['#d32f2f', '#fff', '0% offline'], ['rgba(237,108,2,0.16)', '#b45309', 'Reduced'], ['#fff', 'rgba(0,0,0,0.7)', '100%'], ['rgba(0,0,0,0.04)', 'rgba(0,0,0,0.35)', 'Doesn’t run']].map(([bg, fg, l]) => <span key={l} style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: 12 }}><span style={{ width: 16, height: 12, borderRadius: 3, background: bg, border: '1px solid rgba(0,0,0,0.12)' }}></span>{l}</span>)}
      </div>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', minWidth: 900, borderCollapse: 'collapse', tableLayout: 'fixed' }}>
          <colgroup><col style={{ width: 240 }}/><col style={{ width: 130 }}/>{P1_F.map(f => <col key={f}/>)}</colgroup>
          <thead><tr><th style={p1th}>Process</th><th style={p1th}>Type</th>{P1_F.map(f => <th key={f} style={p1th}><FactoryTag f={f}/></th>)}</tr></thead>
          <tbody>{procs.map(p => (
            <tr key={p.id}>
              <td style={{ ...p1td, fontWeight: 500, height: 52 }}>{p.name}</td>
              <td style={p1td}><TypeChip t={p.type}/></td>
              {P1_F.map(f => { const x = p.factories[f]; const v = x.capacity; const col = capColor(v);
                if (!x.runs) return <td key={f} style={{ ...p1td, color: 'rgba(0,0,0,0.3)', fontSize: 12 }}>Doesn’t run here</td>;
                return (
                  <td key={f} style={{ ...p1td, background: v === 0 ? 'rgba(211,47,47,0.06)' : v < 100 ? 'rgba(237,108,2,0.05)' : 'transparent' }} title={v < 100 ? `${x.reason || 'No reason given'}${x.recovery ? ' · expected recovery ' + x.recovery : ''}` : ''}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <div style={{ flex: 1, height: 6, borderRadius: 3, background: 'rgba(0,0,0,0.08)', overflow: 'hidden' }}><div style={{ width: v + '%', height: '100%', background: col }}></div></div>
                      <b style={{ width: 38, textAlign: 'right', fontWeight: v < 100 ? 600 : 400, fontSize: 13, color: v === 100 ? 'rgba(0,0,0,0.6)' : col }}>{v}%</b>
                    </div>
                    {v < 100 && <div style={{ fontSize: 11, color: v === 0 ? '#c62828' : '#b45309', marginTop: 3, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{v === 0 ? 'Offline' : 'Reduced'}{x.recovery ? ` · until ${x.recovery.replace(', 2026', '')}` : ''}</div>}
                    {v > 0 && <div style={{ fontSize: 11, color: 'rgba(0,0,0,0.5)', marginTop: 2, whiteSpace: 'nowrap' }}>{p1Today(x).remainingPct}% left today</div>}
                  </td>); })}
            </tr>))}</tbody>
        </table>
      </div>
    </div>
  );
}

Object.assign(window, { P1ProcessesList, P1ProcessDetail, P1ProcessCapacity, TypeChip, capColor });

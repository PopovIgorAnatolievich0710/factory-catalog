/* global p1TransferItems, P1_TRANSFER_REASONS, p1DailyPlan, P1_PLAN_DAYS, TypeChip, capColor, P1_UOM_SHORT, React, Icon, Button, Chip, P1_F, P1_STORE, P1_CAN, P1_NOW, P1_ROUTING, P1_DECISIONS, useP1Store, useP1Ctx, p1Bump, p1Log,
   p1th, p1td, p1mono, p1card, p1select, p1input, FactoryTag, ReadOnlyNote, P1Tile, P1Search, P1Card, SourceBadge, FACTORIES */
const { useState: useStateP1O } = React;
const fColor = (f) => (FACTORIES.find(x => x.id === f) || {}).color || '#607d8b';

/* ================= Routing dashboard ================= */
function P1RoutingDashboard() {
  const [fac, setFac] = useStateP1O('all'); const [country, setCountry] = useStateP1O('All countries'); const [range, setRange] = useStateP1O('30');
  const R = P1_ROUTING;
  const k = (range === '7' ? 0.24 : range === '90' ? 2.9 : 1) * (fac === 'all' ? 1 : (R.shares[fac].actual / 100)) * (country === 'All countries' ? 1 : 0.18);
  const jitter = (v, d) => +(v + ((fac + country + range).length % 5 - 2) * d).toFixed(1);
  const shown = fac === 'all' ? P1_F : [fac];
  const max = Math.max(...shown.map(f => Math.max(R.shares[f].target, R.shares[f].actual))) * 1.15;
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
        <select value={fac} onChange={e => setFac(e.target.value)} style={p1select}><option value="all">All factories</option>{P1_F.map(f => <option key={f} value={f}>{f}</option>)}</select>
        <select value={country} onChange={e => setCountry(e.target.value)} style={p1select}>{R.countries.map(c => <option key={c}>{c}</option>)}</select>
        <select value={range} onChange={e => setRange(e.target.value)} style={p1select}><option value="7">Last 7 days</option><option value="30">Last 30 days</option><option value="90">Last 90 days</option></select>
        <div style={{ flex: 1 }}></div><span style={{ fontSize: 12, color: 'rgba(0,0,0,0.55)' }}>Updated {P1_NOW}</span>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))', gap: 12 }}>
        <P1Tile icon="alt_route" label="Orders routed" value={Math.round(R.tiles.routed * k).toLocaleString()} sub={`last ${range} days`}/>
        <P1Tile icon="swap_horiz" label="Transfer rate" value={jitter(R.tiles.transfer_rate, 0.3) + '%'} sub="items moved between factories" color="#ed6c02"/>
        <P1Tile icon="schedule" label="On time vs ETA" value={jitter(R.tiles.on_time, 0.4) + '%'} sub="delivered by promised ETA" color="#2e7d32"/>
        <P1Tile icon="savings" label="Avg cost vs cheapest" value={'+' + jitter(R.tiles.cost_vs_cheapest, 0.2) + '%'} sub="above cheapest feasible" color="#6a1b9a"/>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1.4fr) minmax(0, 1fr)', gap: 16 }}>
        <P1Card title="Target vs actual share" sub="Target = default balance with SKU overrides; actual = routed items.">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            {shown.map(f => { const s = R.shares[f]; const d = +(s.actual - s.target).toFixed(1); return (
              <div key={f} style={{ display: 'grid', gridTemplateColumns: '48px 1fr 70px', gap: 12, alignItems: 'center' }}>
                <FactoryTag f={f}/>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}><div style={{ height: 10, width: `${s.target / max * 100}%`, background: 'rgba(0,0,0,0.14)', borderRadius: 3 }}></div><span style={{ fontSize: 11, color: 'rgba(0,0,0,0.55)' }}>{s.target}% target</span></div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}><div style={{ height: 14, width: `${s.actual / max * 100}%`, background: fColor(f), borderRadius: 3 }}></div><span style={{ fontSize: 12, fontWeight: 500 }}>{s.actual}% actual</span></div>
                </div>
                <span style={{ fontSize: 12, fontWeight: 500, textAlign: 'right', color: Math.abs(d) > 2 ? '#b45309' : 'rgba(0,0,0,0.55)' }}>{d > 0 ? '+' : ''}{d} pt</span>
              </div>); })}
          </div>
        </P1Card>
        <P1Card title="Shadow mode" sub="New balancer runs alongside the legacy balancer; legacy still routes shadow orders.">
          <div style={{ display: 'flex', gap: 22, alignItems: 'center', marginBottom: 16, flexWrap: 'wrap' }}>
            <div><div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.55)' }}>Agreement rate</div><div style={{ fontSize: 32, fontWeight: 300 }}>{R.shadow.agreement}%</div><div style={{ fontSize: 12, color: 'rgba(0,0,0,0.55)' }}>of {Math.round(R.shadow.compared * k).toLocaleString()} compared items</div></div>
            <div><div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.55)' }}>Estimated savings</div><div style={{ fontSize: 32, fontWeight: 300, color: '#2e7d32' }}>${Math.round(R.shadow.savings * k).toLocaleString()}</div><div style={{ fontSize: 12, color: 'rgba(0,0,0,0.55)' }}>if new decisions had been live</div></div>
          </div>
          <div style={{ height: 8, borderRadius: 4, background: 'rgba(0,0,0,0.08)', overflow: 'hidden', marginBottom: 14 }}><div style={{ width: R.shadow.agreement + '%', height: '100%', background: '#2e7d32' }}></div></div>
          <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.55)', marginBottom: 6 }}>Why they disagree</div>
          {R.shadow.disagree_top.map(d => <div key={d.reason} style={{ display: 'flex', gap: 10, fontSize: 13, padding: '5px 0', borderBottom: '1px solid rgba(0,0,0,0.05)' }}><span style={{ flex: 1 }}>{d.reason}</span><b style={{ fontWeight: 500 }}>{d.share}%</b></div>)}
        </P1Card>
      </div>
    </div>
  );
}

/* ================= Balancer decision log ================= */
function ModeChip({ m }) {
  const c = m === 'Live' ? ['rgba(46,125,50,0.12)', '#1b5e20', 'bolt'] : m === 'Shadow' ? ['rgba(81,45,168,0.1)', '#512da8', 'visibility'] : ['rgba(237,108,2,0.14)', '#b45309', 'undo'];
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, height: 22, padding: '0 9px', borderRadius: 11, fontSize: 12, fontWeight: 500, background: c[0], color: c[1] }}><Icon name={c[2]} size={14}/>{m}</span>;
}
function P1BalancerLog() {
  const S = useP1Store(); const [q, setQ] = useStateP1O(''); const [mode, setMode] = useStateP1O('all');
  const orders = P1_DECISIONS.filter(o => (!q || o.order.toLowerCase().includes(q.toLowerCase())) && (mode === 'all' || o.items.some(i => i.mode === mode)));
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
        <P1Search value={q} onChange={setQ} placeholder="Search by order ID (e.g. ORD-884133)…" width={380}/>
        <select value={mode} onChange={e => setMode(e.target.value)} style={p1select}><option value="all">All modes</option><option>Live</option><option>Shadow</option><option>Fallback</option></select>
      </div>
      {orders.map(o => (
        <div key={o.order} style={p1card}>
          <div style={{ padding: '12px 18px', display: 'flex', gap: 14, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
            <b style={{ ...p1mono, fontSize: 14, fontWeight: 600 }}>{o.order}</b>
            <span style={{ fontSize: 13, color: 'rgba(0,0,0,0.6)' }}>{o.placed}</span>
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 13, color: 'rgba(0,0,0,0.7)' }}><Icon name="place" size={15}/>{o.dest}</span>
            <div style={{ flex: 1 }}></div><span style={{ fontSize: 12, color: 'rgba(0,0,0,0.55)' }}>{o.items.length} item{o.items.length > 1 ? 's' : ''}</span>
          </div>
          {o.items.map((it, i) => { const sku = S.skus[it.sku]; const win = it.cands.find(c => c.factory === it.winner); return (
            <div key={i} style={{ padding: '14px 18px', borderBottom: i < o.items.length - 1 ? '1px dashed rgba(0,0,0,0.1)' : 'none' }}>
              <div style={{ display: 'flex', gap: 18, alignItems: 'center', flexWrap: 'wrap', marginBottom: 10 }}>
                <div style={{ minWidth: 220, flex: 1 }}><div style={{ fontWeight: 500 }}>{sku ? sku.name : it.sku}</div><div style={{ ...p1mono, fontSize: 11.5, color: 'rgba(0,0,0,0.5)' }}>{it.sku}</div></div>
                <div><div style={{ fontSize: 10, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.5)' }}>Winner</div><FactoryTag f={it.winner}/></div>
                <div><div style={{ fontSize: 10, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.5)' }}>Packing</div><FactoryTag f={it.packing}/></div>
                <div><div style={{ fontSize: 10, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.5)' }}>Total cost</div><b style={{ ...p1mono, fontSize: 14 }}>{win.total != null ? '$' + win.total.toFixed(2) : '—'}</b></div>
                <div><div style={{ fontSize: 10, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.5)' }}>ETA</div><b style={{ fontWeight: 500 }}>{win.eta}</b></div>
                <div><div style={{ fontSize: 10, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'rgba(0,0,0,0.5)' }}>Mode</div><ModeChip m={it.mode}/></div>
                {it.legacy && <span style={{ fontSize: 12, color: '#512da8' }}>Legacy picked {it.legacy}</span>}
              </div>
              <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid rgba(0,0,0,0.08)', borderRadius: 6 }}>
                <thead><tr><th style={p1th}>Candidate</th><th style={p1th}>Production cost</th><th style={p1th}>Shipping cost</th><th style={p1th}>Total</th><th style={p1th}>ETA</th><th style={p1th}>Excluded because</th></tr></thead>
                <tbody>{it.cands.map(c => { const w = c.factory === it.winner; return (
                  <tr key={c.factory} style={{ background: w ? 'rgba(46,125,50,0.06)' : 'transparent', opacity: c.excluded ? 0.75 : 1 }}>
                    <td style={p1td}><span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><FactoryTag f={c.factory}/>{w && <Icon name="emoji_events" size={16} sx={{ color: '#2e7d32' }}/>}</span></td>
                    <td style={{ ...p1td, ...p1mono }}>{c.prod != null ? '$' + c.prod.toFixed(2) : '—'}</td>
                    <td style={{ ...p1td, ...p1mono }}>{c.ship != null ? '$' + c.ship.toFixed(2) : '—'}</td>
                    <td style={{ ...p1td, ...p1mono, fontWeight: w ? 600 : 400 }}>{c.total != null ? '$' + c.total.toFixed(2) : '—'}</td>
                    <td style={p1td}>{c.eta}</td>
                    <td style={{ ...p1td, fontSize: 13, color: c.excluded ? '#c62828' : 'rgba(0,0,0,0.4)' }}>{c.excluded || (w ? 'Selected — lowest feasible cost' : 'Feasible, higher cost')}</td>
                  </tr>); })}</tbody>
              </table>
            </div>); })}
        </div>
      ))}
      {!orders.length && <div style={{ ...p1card, padding: 32, textAlign: 'center', color: 'rgba(0,0,0,0.5)' }}>No decisions for that order ID.</div>}
    </div>
  );
}


/* ================= Daily plan (shift manager) ================= */
function P1DailyPlan() {
  const ctx = useP1Ctx(); useP1Store();
  const isFM = ctx.role === 'factory_manager';
  const [fac, setFac] = useStateP1O(isFM ? ctx.factory : 'NZ');
  const [day, setDay] = useStateP1O(0);
  const [open, setOpen] = useStateP1O(null);
  const f = isFM ? ctx.factory : fac;
  const rows = p1DailyPlan(f, day);
  const load = r => r.available ? Math.round((r.done + r.planned) / r.available * 100) : 999;
  const over = rows.filter(r => r.done + r.planned > r.available);
  const totalPlanned = rows.reduce((n, r) => n + r.planned, 0);
  const sorted = rows.slice().sort((a, b) => load(b) - load(a));
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        {isFM ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8, fontSize: 14 }}>Your factory <FactoryTag f={f}/></span>
          : <select value={fac} onChange={e => { setFac(e.target.value); setOpen(null); }} style={p1select}>{P1_F.map(x => <option key={x} value={x}>Factory {x}</option>)}</select>}
        <div style={{ display: 'inline-flex', padding: 3, background: '#eceff1', borderRadius: 8 }}>
          {P1_PLAN_DAYS.map((d, i) => <button key={d} onClick={() => { setDay(i); setOpen(null); }} style={{ border: 'none', cursor: 'pointer', padding: '6px 14px', borderRadius: 6, fontFamily: 'Roboto', fontSize: 13, fontWeight: 500, background: day === i ? '#fff' : 'transparent', color: day === i ? '#1976d2' : 'rgba(0,0,0,0.6)', boxShadow: day === i ? '0 1px 3px rgba(0,0,0,0.15)' : 'none' }}>{i === 0 ? 'Today' : i === 1 ? 'Tomorrow' : d.replace(', 2026', '')}</button>)}
        </div>
        <div style={{ flex: 1 }}></div>
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'rgba(0,0,0,0.6)' }}><Icon name="info" size={15}/>Balanced, not produced yet · from the selected production quotes</span>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 12 }}>
        <P1Tile icon="assignment" label="Items to produce" value={totalPlanned.toLocaleString()} sub={P1_PLAN_DAYS[day]}/>
        <P1Tile icon="precision_manufacturing" label="Processes in use" value={rows.length}/>
        <P1Tile icon="warning" label="Over capacity" value={over.length} sub={over.length ? 'processes need attention' : 'all within capacity'} color={over.length ? '#d32f2f' : '#2e7d32'}/>
      </div>
      <div style={p1card}>
        <table style={{ width: '100%', borderCollapse: 'collapse', tableLayout: 'fixed' }}>
          <colgroup><col style={{ width: 36 }}/><col style={{ width: 220 }}/><col style={{ width: 120 }}/><col style={{ width: 120 }}/><col style={{ width: 130 }}/><col style={{ width: 110 }}/><col style={{ width: 110 }}/><col/></colgroup>
          <thead><tr><th style={p1th}></th><th style={p1th}>Process</th><th style={p1th}>Type</th><th style={p1th}>Items to do</th><th style={p1th}>Quantity</th><th style={p1th}>Done</th><th style={p1th}>Available</th><th style={p1th}>Load</th></tr></thead>
          <tbody>{sorted.map(r => { const l = load(r); const isOver = r.done + r.planned > r.available; const isOpen = open === r.proc.id; const col = isOver ? '#d32f2f' : l >= 85 ? '#ed6c02' : '#2e7d32'; return (
            <React.Fragment key={r.proc.id}>
              <tr onClick={() => setOpen(isOpen ? null : r.proc.id)} style={{ cursor: 'pointer', background: isOver ? 'rgba(211,47,47,0.04)' : 'transparent' }}>
                <td style={p1td}><Icon name={isOpen ? 'expand_more' : 'chevron_right'} size={18} sx={{ color: 'rgba(0,0,0,0.5)' }}/></td>
                <td style={{ ...p1td, fontWeight: 500 }}>{r.proc.name}</td>
                <td style={p1td}><TypeChip t={r.proc.type}/></td>
                <td style={{ ...p1td, ...p1mono, fontWeight: 500 }}>{r.planned.toLocaleString()}</td>
                <td style={{ ...p1td, ...p1mono }}>{r.qty.toLocaleString()} {P1_UOM_SHORT[r.proc.uom] || r.proc.uom}</td>
                <td style={{ ...p1td, ...p1mono, color: 'rgba(0,0,0,0.6)' }}>{r.done.toLocaleString()}</td>
                <td style={{ ...p1td, ...p1mono }}>{r.available.toLocaleString()}</td>
                <td style={p1td}><div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <div style={{ flex: 1, height: 8, borderRadius: 4, background: 'rgba(0,0,0,0.08)', overflow: 'hidden' }}><div style={{ width: Math.min(100, l) + '%', height: '100%', background: col }}></div></div>
                  <b style={{ width: 46, textAlign: 'right', fontSize: 13, fontWeight: 600, color: col }}>{r.available ? l + '%' : '—'}</b>
                  {isOver && <span style={{ fontSize: 11, fontWeight: 500, color: '#c62828', whiteSpace: 'nowrap' }}>Over by {(r.done + r.planned - r.available).toLocaleString()}</span>}
                </div></td>
              </tr>
              {isOpen && <tr><td></td><td colSpan="7" style={{ padding: '4px 16px 14px' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid rgba(0,0,0,0.08)' }}>
                  <thead><tr><th style={p1th}>Order ID</th><th style={p1th}>Item</th><th style={p1th}>SKU</th><th style={p1th}>Production quote</th><th style={p1th}>Due</th></tr></thead>
                  <tbody>{r.items.map((it, i) => <tr key={i}>
                    <td style={{ ...p1td, ...p1mono, color: '#1976d2' }}>{it.orderId}</td><td style={{ ...p1td, ...p1mono }}>{it.itemId}</td>
                    <td style={p1td}><div style={{ fontWeight: 500 }}>{it.skuName}</div><div style={{ ...p1mono, fontSize: 11.5, color: 'rgba(0,0,0,0.5)' }}>{it.sku}</div></td>
                    <td style={{ ...p1td, ...p1mono, fontSize: 12, color: '#1976d2' }}>{it.quoteId}</td><td style={p1td}>{it.due.replace(', 2026', '')}</td></tr>)}
                    {r.planned > r.items.length && <tr><td colSpan="5" style={{ ...p1td, fontSize: 12, color: 'rgba(0,0,0,0.5)' }}>+ {(r.planned - r.items.length).toLocaleString()} more items</td></tr>}
                  </tbody>
                </table>
              </td></tr>}
            </React.Fragment>); })}
            {!rows.length && <tr><td colSpan="8" style={{ ...p1td, textAlign: 'center', padding: 28, color: 'rgba(0,0,0,0.5)' }}>Nothing planned for this day.</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  );
}


/* ================= Transfers ================= */
function P1Transfers() {
  const S = useP1Store(); const ctx = useP1Ctx();
  const isFM = ctx.role === 'factory_manager';
  const [tab, setTab] = useStateP1O('items');
  const blank = { order: '', sup: isFM ? ctx.factory : '', page: '', event: '', site: '', due: '' };
  const [flt, setFlt] = useStateP1O(blank); const [applied, setApplied] = useStateP1O(blank);
  const [page, setPage] = useStateP1O(0); const [sel, setSel] = useStateP1O({});
  const [to, setTo] = useStateP1O(''); const [toPage, setToPage] = useStateP1O(''); const [reason, setReason] = useStateP1O(''); const [note, setNote] = useStateP1O(''); const [preview, setPreview] = useStateP1O(false);
  const [lq, setLq] = useStateP1O(''); const [lf, setLf] = useStateP1O('all');
  const setF = (k, v) => setFlt(o => ({ ...o, [k]: v, ...(k === 'sup' ? { page: '' } : {}) }));
  const search = () => { setApplied(flt); setPage(0); setSel({}); setPreview(false); };
  const reset = () => { setFlt(blank); setApplied(blank); setPage(0); setSel({}); setPreview(false); };
  const a = applied;
  const all = p1TransferItems().filter(it => (!a.order || it.orderId.toLowerCase().includes(a.order.trim().toLowerCase())) && (!a.sup || it.factory === a.sup) && (!a.page || it.page === a.page)
    && (!a.event || it.event === a.event) && (!a.site || it.site === a.site) && (!a.due || it.due === a.due));
  const canItem = it => P1_CAN.transfer(ctx, it.factory);
  const PS = 10; const pages = Math.max(1, Math.ceil(all.length / PS)); const pg = Math.min(page, pages - 1);
  const rows = all.slice(pg * PS, pg * PS + PS);
  const selected = Object.values(sel); const n = selected.length;
  const toggle = it => setSel(o => { const c = { ...o }; if (c[it.key]) delete c[it.key]; else c[it.key] = it; return c; });
  const pageRows = rows.filter(canItem);
  const pageAll = pageRows.length > 0 && pageRows.every(it => sel[it.key]);
  const togglePage = () => setSel(o => { const c = { ...o }; pageRows.forEach(it => { if (pageAll) delete c[it.key]; else c[it.key] = it; }); return c; });
  const selectAll = () => setSel(Object.fromEntries(all.filter(canItem).map(it => [it.key, it])));
  const fromSet = [...new Set(selected.map(it => it.factory))];
  const samePlace = it => it.factory === to && it.page === toPage;
  const delta = it => it.factory === to ? 0 : +(((it.orderId.charCodeAt(9) + to.charCodeAt(0)) % 9 - 3) * 0.35).toFixed(2);
  const newQuote = it => it.quoteId.replace(/-[A-Z]{2}$/, '-' + to);
  const confirm = () => {
    const when = P1_NOW; const moving = selected.filter(it => !samePlace(it));
    moving.forEach(it => {
      S.transferred[it.key] = { to, page: toPage };
      S.transferLog.unshift({ when, who: ctx.user.name, orderId: it.orderId, itemId: it.itemId, sku: it.sku, from: it.factory, fromPage: it.page, to, toPage, reason, note, oldQuote: it.quoteId, newQuote: newQuote(it), costDelta: delta(it), etaFrom: it.due.replace(', 2026', ''), etaTo: it.due.replace(', 2026', ''), by: 'Manual' });
      p1Log('Transfer', `${it.orderId} · ${it.itemId}`, ctx.user.name, 'Supplier · page', `${it.factory} · ${it.page}`, `${to} · ${toPage}`);
    });
    ctx.toast(`${moving.length} item${moving.length !== 1 ? 's' : ''} moved to ${to} · page ${toPage} — re-quoted and logged`);
    setSel({}); setPreview(false); setReason(''); setNote(''); p1Bump();
  };
  const log = S.transferLog.filter(r => (lf === 'all' || r.from === lf || r.to === lf) && (!lq || (r.orderId + r.sku + r.who + r.reason + (r.fromPage || '') + (r.toPage || '')).toLowerCase().includes(lq.toLowerCase())));
  const chip = (b) => <span style={{ fontSize: 11, padding: '2px 8px', borderRadius: 10, fontWeight: 500, background: b === 'Balancer' ? 'rgba(81,45,168,0.1)' : 'rgba(0,0,0,0.06)', color: b === 'Balancer' ? '#512da8' : 'rgba(0,0,0,0.7)' }}>{b}</span>;
  const pgTag = (p) => p ? <span style={{ ...p1mono, fontSize: 12, padding: '1px 6px', borderRadius: 4, background: 'rgba(0,0,0,0.05)' }}>{p}</span> : null;
  const lbl = (t, el) => <label style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>{t}{el}</label>;
  const fsel = { ...p1select, minWidth: 130 };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
        <div style={{ display: 'inline-flex', padding: 3, background: '#eceff1', borderRadius: 8 }}>
          {[['items', 'Find & transfer'], ['log', `Transfer log (${S.transferLog.length})`]].map(([k, l]) => <button key={k} onClick={() => setTab(k)} style={{ border: 'none', cursor: 'pointer', padding: '6px 14px', borderRadius: 6, fontFamily: 'Roboto', fontSize: 13, fontWeight: 500, background: tab === k ? '#fff' : 'transparent', color: tab === k ? '#1976d2' : 'rgba(0,0,0,0.6)', boxShadow: tab === k ? '0 1px 3px rgba(0,0,0,0.15)' : 'none' }}>{l}</button>)}
        </div>
        <div style={{ flex: 1 }}></div>
        {tab === 'items' && isFM && <ReadOnlyNote text={`You can transfer items from ${ctx.factory}; operations can transfer from any supplier`}/>}
      </div>

      {tab === 'items' && <>
        <div style={{ ...p1card, padding: 16 }}>
          <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end', flexWrap: 'wrap' }}>
            {lbl('Order ID', <input value={flt.order} onChange={e => setF('order', e.target.value)} onKeyDown={e => e.key === 'Enter' && search()} placeholder="e.g. ORD-884120" style={{ ...p1input, width: 170 }}/>)}
            {lbl('Supplier', <select value={flt.sup} onChange={e => setF('sup', e.target.value)} style={fsel}><option value="">All suppliers</option>{P1_F.map(x => <option key={x} value={x}>{x}</option>)}</select>)}
            {lbl('Page', <select value={flt.page} onChange={e => setF('page', e.target.value)} disabled={!flt.sup} title={!flt.sup ? 'Pick a supplier first' : ''} style={fsel}><option value="">All pages</option>{flt.sup && p1Pages(flt.sup).map(x => <option key={x} value={x}>{x}</option>)}</select>)}
            {lbl('Event', <select value={flt.event} onChange={e => setF('event', e.target.value)} style={fsel}><option value="">All events</option>{P1_EVENTS.map(x => <option key={x}>{x}</option>)}</select>)}
            {lbl('Site', <select value={flt.site} onChange={e => setF('site', e.target.value)} style={fsel}><option value="">All sites</option>{P1_SITES.map(x => <option key={x}>{x}</option>)}</select>)}
            {lbl('Due day', <select value={flt.due} onChange={e => setF('due', e.target.value)} style={fsel}><option value="">Any day</option>{P1_PLAN_DAYS.map(x => <option key={x} value={x}>{x.replace(', 2026', '')}</option>)}</select>)}
            <Button variant="contained" startIcon="search" onClick={search}>Search</Button>
            <Button variant="text" onClick={reset}>Reset</Button>
          </div>
        </div>

        <div style={p1card}>
          <div style={{ padding: '12px 16px', display: 'flex', gap: 10, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap', fontSize: 13 }}>
            <span style={{ color: 'rgba(0,0,0,0.6)' }}><b style={{ fontWeight: 500, color: 'rgba(0,0,0,0.87)' }}>{all.length}</b> open items (balanced, not produced)</span>
            <div style={{ flex: 1 }}></div>
            {n > 0 && <span><b style={{ fontWeight: 500 }}>{n}</b> selected across pages · <a href="#" onClick={e => { e.preventDefault(); setSel({}); }} style={{ color: '#1976d2' }}>Clear</a></span>}
            {all.some(canItem) && <Button variant="text" size="small" onClick={selectAll}>Select all {all.filter(canItem).length}</Button>}
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse', tableLayout: 'fixed' }}>
            <colgroup><col style={{ width: 44 }}/><col style={{ width: 120 }}/><col style={{ width: 64 }}/><col/><col style={{ width: 92 }}/><col style={{ width: 96 }}/><col style={{ width: 110 }}/><col style={{ width: 70 }}/><col style={{ width: 80 }}/></colgroup>
            <thead><tr><th style={p1th}><input type="checkbox" disabled={!pageRows.length} checked={pageAll} onChange={togglePage} title="Select this page"/></th><th style={p1th}>Order ID</th><th style={p1th}>Item</th><th style={p1th}>SKU</th><th style={p1th}>Supplier</th><th style={p1th}>Page</th><th style={p1th}>Event</th><th style={p1th}>Site</th><th style={p1th}>Due</th></tr></thead>
            <tbody>{rows.map(it => { const ok = canItem(it); return (
              <tr key={it.key} style={{ height: 52, background: sel[it.key] ? 'rgba(25,118,210,0.05)' : 'transparent', opacity: ok ? 1 : 0.6 }}>
                <td style={p1td}><input type="checkbox" disabled={!ok} title={ok ? '' : `Only the ${it.factory} manager or operations`} checked={!!sel[it.key]} onChange={() => toggle(it)}/></td>
                <td style={{ ...p1td, ...p1mono, color: '#1976d2' }}>{it.orderId}</td><td style={{ ...p1td, ...p1mono }}>{it.itemId}</td>
                <td style={p1td}><div style={{ fontWeight: 500, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{it.skuName}</div><div style={{ ...p1mono, fontSize: 11.5, color: 'rgba(0,0,0,0.5)' }}>{it.sku}</div></td>
                <td style={p1td}><FactoryTag f={it.factory}/></td><td style={p1td}>{pgTag(it.page)}</td>
                <td style={{ ...p1td, fontSize: 13, color: it.event === 'Regular' ? 'rgba(0,0,0,0.6)' : '#b45309', fontWeight: it.event === 'Regular' ? 400 : 500 }}>{it.event}</td>
                <td style={{ ...p1td, fontSize: 13 }}>{it.site}</td><td style={{ ...p1td, fontSize: 13 }}>{it.due.replace(', 2026', '')}</td>
              </tr>); })}
              {!rows.length && <tr><td colSpan="9" style={{ ...p1td, textAlign: 'center', padding: 28, color: 'rgba(0,0,0,0.5)' }}>No open items match this search.</td></tr>}</tbody>
          </table>
          <div style={{ padding: '10px 16px', display: 'flex', alignItems: 'center', gap: 10, borderTop: '1px solid rgba(0,0,0,0.08)', fontSize: 13, color: 'rgba(0,0,0,0.6)' }}>
            <span>{all.length ? `${pg * PS + 1}–${Math.min(all.length, pg * PS + PS)} of ${all.length}` : '0 items'}</span><div style={{ flex: 1 }}></div>
            <Button variant="text" size="small" startIcon="chevron_left" disabled={pg === 0} onClick={() => setPage(pg - 1)}>Previous</Button>
            <span>Page {pg + 1} of {pages}</span>
            <Button variant="text" size="small" disabled={pg >= pages - 1} onClick={() => setPage(pg + 1)}>Next</Button>
          </div>
        </div>

        {n > 0 && <P1Card title={`Transfer ${n} item${n > 1 ? 's' : ''}${fromSet.length === 1 ? ' from ' + fromSet[0] : ' from ' + fromSet.length + ' suppliers'}`} sub="Pick the supplier and the production page to move them to. The balancer re-quotes each item before you confirm.">
          <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end', flexWrap: 'wrap' }}>
            {lbl('To supplier', <select value={to} onChange={e => { setTo(e.target.value); setToPage(''); setPreview(false); }} style={fsel}><option value="">Supplier…</option>{P1_F.map(x => <option key={x} value={x}>{x}</option>)}</select>)}
            {lbl('To page', <select value={toPage} onChange={e => { setToPage(e.target.value); setPreview(false); }} disabled={!to} style={fsel}><option value="">Page…</option>{to && p1Pages(to).map(x => <option key={x} value={x}>{x}</option>)}</select>)}
            {lbl('Reason *', <select value={reason} onChange={e => setReason(e.target.value)} style={{ ...fsel, minWidth: 190 }}><option value="">Reason…</option>{P1_TRANSFER_REASONS.map(r => <option key={r}>{r}</option>)}</select>)}
            {lbl(reason === 'Other' ? 'Note *' : 'Note', <input value={note} onChange={e => setNote(e.target.value)} placeholder="e.g. Laser down until Nov 28" style={{ ...p1input, minWidth: 260 }}/>)}
            <Button variant="outlined" disabled={!to || !toPage || !reason || (reason === 'Other' && !note.trim())} onClick={() => setPreview(true)}>Preview re-quote</Button>
          </div>
          {preview && to && toPage && <div style={{ marginTop: 16 }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid rgba(0,0,0,0.08)' }}>
              <thead><tr><th style={p1th}>Order · item</th><th style={p1th}>From</th><th style={p1th}>To</th><th style={p1th}>Quote (old → new)</th><th style={p1th}>Cost change</th><th style={p1th}>Capacity at {to}</th></tr></thead>
              <tbody>{selected.slice(0, 8).map(it => { const dd = delta(it); const same = samePlace(it); return <tr key={it.key} style={{ opacity: same ? 0.5 : 1 }}>
                <td style={{ ...p1td, ...p1mono }}>{it.orderId} · {it.itemId}</td>
                <td style={p1td}><span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}><FactoryTag f={it.factory}/>{pgTag(it.page)}</span></td>
                <td style={p1td}><span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}><FactoryTag f={to}/>{pgTag(toPage)}</span></td>
                {same ? <td colSpan="3" style={{ ...p1td, fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>Already on this page — skipped</td> : <>
                <td style={{ ...p1td, ...p1mono, fontSize: 12 }}><span style={{ color: 'rgba(0,0,0,0.5)' }}>{it.quoteId}</span><br/><span style={{ color: '#1976d2' }}>{newQuote(it)}</span></td>
                <td style={{ ...p1td, ...p1mono, color: dd > 0 ? '#c62828' : dd < 0 ? '#2e7d32' : 'rgba(0,0,0,0.6)' }}>{dd > 0 ? '+' : ''}${dd.toFixed(2)}</td>
                <td style={{ ...p1td, fontSize: 12 }}>{dd > 1 ? <span style={{ color: '#b45309' }}>Tight — 9% left today</span> : <span style={{ color: '#2e7d32' }}>OK</span>}</td></>}</tr>; })}
                {n > 8 && <tr><td colSpan="6" style={{ ...p1td, fontSize: 12, color: 'rgba(0,0,0,0.5)' }}>+ {n - 8} more items</td></tr>}</tbody>
            </table>
            <div style={{ display: 'flex', gap: 10, marginTop: 12, alignItems: 'center' }}>
              <Button variant="contained" startIcon="swap_horiz" disabled={selected.every(samePlace)} onClick={confirm}>Confirm transfer</Button>
              <Button variant="text" onClick={() => setPreview(false)}>Cancel</Button>
              <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.55)' }}>Old quotes are marked transferred, new ones selected, and TG Supplier moves the items to page {toPage}. Every move is logged.</span>
            </div>
          </div>}
        </P1Card>}
      </>}

      {tab === 'log' && <div style={p1card}>
        <div style={{ padding: '12px 16px', display: 'flex', gap: 10, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
          <P1Search value={lq} onChange={setLq} placeholder="Search order, SKU, page, user or reason…" width={340}/>
          <select value={lf} onChange={e => setLf(e.target.value)} style={p1select}><option value="all">All suppliers</option>{P1_F.map(x => <option key={x} value={x}>{x}</option>)}</select>
        </div>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr><th style={p1th}>When</th><th style={p1th}>Order · item</th><th style={p1th}>Move (supplier · page)</th><th style={p1th}>Reason</th><th style={p1th}>By</th><th style={p1th}>Quotes (old → new)</th><th style={p1th}>Cost · ETA</th></tr></thead>
          <tbody>{log.map((r, i) => <tr key={i}>
            <td style={{ ...p1td, ...p1mono, fontSize: 12, color: 'rgba(0,0,0,0.6)', whiteSpace: 'nowrap' }}>{r.when}</td>
            <td style={p1td}><div style={{ ...p1mono, color: '#1976d2' }}>{r.orderId} · {r.itemId}</div><div style={{ ...p1mono, fontSize: 11.5, color: 'rgba(0,0,0,0.5)' }}>{r.sku}</div></td>
            <td style={p1td}><span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}><FactoryTag f={r.from}/>{pgTag(r.fromPage)}<Icon name="arrow_forward" size={15} sx={{ color: 'rgba(0,0,0,0.5)' }}/><FactoryTag f={r.to}/>{pgTag(r.toPage)}</span></td>
            <td style={p1td}><div style={{ fontWeight: 500, fontSize: 13 }}>{r.reason}</div>{r.note && <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>{r.note}</div>}</td>
            <td style={p1td}><div style={{ fontSize: 13 }}>{r.who}</div>{chip(r.by)}</td>
            <td style={{ ...p1td, fontSize: 12 }}><a href="#" onClick={e => e.preventDefault()} style={{ ...p1mono, color: 'rgba(0,0,0,0.55)', textDecoration: 'line-through' }}>{r.oldQuote}</a><br/><a href="#" onClick={e => e.preventDefault()} style={{ ...p1mono, color: '#1976d2' }}>{r.newQuote}</a></td>
            <td style={{ ...p1td, fontSize: 12 }}><span style={{ ...p1mono, color: r.costDelta > 0 ? '#c62828' : '#2e7d32' }}>{r.costDelta > 0 ? '+' : ''}${r.costDelta.toFixed(2)}</span><div style={{ color: 'rgba(0,0,0,0.6)' }}>ETA {r.etaFrom} → {r.etaTo}</div></td>
          </tr>)}
          {!log.length && <tr><td colSpan="7" style={{ ...p1td, textAlign: 'center', padding: 28, color: 'rgba(0,0,0,0.5)' }}>No transfers match.</td></tr>}</tbody>
        </table>
      </div>}
    </div>
  );
}

/* ================= Audit log ================= */
function P1AuditLog() {
  const S = useP1Store(); const [q, setQ] = useStateP1O(''); const [ent, setEnt] = useStateP1O('all');
  const seed = [];
  Object.values(S.comps).slice(0, 12).forEach(c => c.history.forEach(h => seed.push({ entity: 'Component', ref: c.erp_id, ...h })));
  Object.values(S.procs).slice(0, 8).forEach(p => p.history.forEach(h => seed.push({ entity: 'Process', ref: p.name, ...h })));
  Object.values(S.skus).slice(0, 6).forEach(s => s.history.forEach(h => seed.push({ entity: 'SKU', ref: s.site_sku, ...h })));
  const rows = [...S.audit, ...seed].filter(r => (ent === 'all' || r.entity === ent) && (!q || (r.ref + r.who + r.field).toLowerCase().includes(q.toLowerCase())));
  return (
    <div style={p1card}>
      <div style={{ padding: '12px 16px', display: 'flex', gap: 10, alignItems: 'center', borderBottom: '1px solid rgba(0,0,0,0.08)', flexWrap: 'wrap' }}>
        <P1Search value={q} onChange={setQ} placeholder="Search item, user or field…"/>
        <select value={ent} onChange={e => setEnt(e.target.value)} style={p1select}><option value="all">All items</option><option>Component</option><option>Process</option><option>SKU</option><option>Default balance</option></select>
      </div>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead><tr><th style={p1th}>When</th><th style={p1th}>Who</th><th style={p1th}>Item</th><th style={p1th}>Field</th><th style={p1th}>Old value</th><th style={p1th}>New value</th></tr></thead>
        <tbody>{rows.slice(0, 80).map((r, i) => (
          <tr key={i}>
            <td style={{ ...p1td, ...p1mono, color: 'rgba(0,0,0,0.6)', whiteSpace: 'nowrap' }}>{r.when}</td>
            <td style={p1td}>{r.who}</td>
            <td style={p1td}><span style={{ fontSize: 11, color: 'rgba(0,0,0,0.5)', marginRight: 6 }}>{r.entity}</span><span style={p1mono}>{r.ref}</span></td>
            <td style={p1td}>{r.field}</td>
            <td style={{ ...p1td, color: 'rgba(0,0,0,0.5)', textDecoration: 'line-through' }}>{r.old}</td>
            <td style={{ ...p1td, fontWeight: 500 }}>{r.nw}</td>
          </tr>))}</tbody>
      </table>
    </div>
  );
}

/* ================= Settings ================= */
function P1Settings() {
  const S = useP1Store(); const ctx = useP1Ctx();
  const canShare = P1_CAN.editDefaultShare(ctx), canSync = P1_CAN.syncNow(ctx);
  const [busy, setBusy] = useStateP1O(null);
  const tot = P1_F.reduce((n, f) => n + S.defaultShare[f], 0);
  const setDef = (f, v) => { const old = S.defaultShare[f]; S.defaultShare[f] = Math.max(0, Math.min(100, parseInt(v, 10) || 0)); p1Log('Default balance', f, ctx.user.name, `Default share · ${f}`, old + '%', S.defaultShare[f] + '%'); p1Bump(); };
  const sync = (k) => { setBusy(k); setTimeout(() => { S.sync[k].last = P1_NOW; S.sync[k].result = 'Success'; setBusy(null); p1Bump(); ctx.toast(`${S.sync[k].label} sync complete`); }, 900); };
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16, maxWidth: 980 }}>
      <P1Card title="Global factory share" sub="The default balance used for every SKU without a factory override. Must total 100%." right={!canShare && <ReadOnlyNote text="Factory catalog admin edits"/>}>
        <div style={{ display: 'flex', gap: 16, alignItems: 'flex-end', flexWrap: 'wrap' }}>
          {P1_F.map(f => (
            <div key={f} style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <FactoryTag f={f}/>
              {canShare ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}><input type="number" min="0" max="100" value={S.defaultShare[f]} onChange={e => setDef(f, e.target.value)} style={{ ...p1input, width: 80, fontSize: 16, textAlign: 'right' }}/>%</span>
                : <span style={{ fontSize: 24, fontWeight: 300 }}>{S.defaultShare[f]}%</span>}
            </div>))}
          <div style={{ paddingBottom: 6 }}>{tot === 100 ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, color: '#2e7d32', fontSize: 13 }}><Icon name="check_circle" size={17}/>Total 100%</span> : <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, color: '#c62828', fontSize: 13, fontWeight: 500 }}><Icon name="error" size={17}/>Total {tot}% — must be 100%</span>}</div>
        </div>
        <div style={{ display: 'flex', height: 10, borderRadius: 5, overflow: 'hidden', marginTop: 16 }}>{P1_F.map(f => <div key={f} style={{ width: (S.defaultShare[f] / Math.max(tot, 100) * 100) + '%', background: fColor(f) }}></div>)}</div>
      </P1Card>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 16 }}>
        {['sap', 'mc'].map(k => { const s = S.sync[k]; return (
          <div key={k} style={{ ...p1card, padding: 18, display: 'flex', flexDirection: 'column', gap: 10 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}><SourceBadge src={k}/><b style={{ fontSize: 16, fontWeight: 500 }}>{s.label} sync</b><div style={{ flex: 1 }}></div><Chip label={s.result} color="success" size="small" icon="check_circle"/></div>
            <div style={{ fontSize: 13, color: 'rgba(0,0,0,0.7)', display: 'grid', gridTemplateColumns: '110px 1fr', rowGap: 6 }}>
              <span style={{ color: 'rgba(0,0,0,0.5)' }}>Schedule</span><span>{s.cadence}</span>
              <span style={{ color: 'rgba(0,0,0,0.5)' }}>Last run</span><span style={p1mono}>{s.last}</span>
              <span style={{ color: 'rgba(0,0,0,0.5)' }}>Result</span><span>{s.detail}</span>
            </div>
            {canSync ? <div><Button variant="outlined" size="small" startIcon={busy === k ? 'hourglass_top' : 'sync'} disabled={busy === k} onClick={() => sync(k)}>{busy === k ? 'Syncing…' : 'Sync now'}</Button></div> : <div><ReadOnlyNote text="Admin can run a sync"/></div>}
          </div>); })}
      </div>
    </div>
  );
}

Object.assign(window, { P1RoutingDashboard, P1BalancerLog, P1AuditLog, P1Settings, P1DailyPlan, P1Transfers });

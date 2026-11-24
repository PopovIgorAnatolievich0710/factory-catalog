/* global React, Icon, IconButton, Avatar, Chip, Button, TextField, Card, Checkbox, ROLES, SITES, FACTORIES, SKUS, STATUS, ITEM_STATUS, StatusPill, VersionChip, WorkflowRibbon */
const { useState: useStateApp, useMemo: useMemoApp } = React;

/* -------- App shell with role switcher -------- */
function CatalogShell({ role, onRole, route, onRoute, density, children, scope, p1 }) {
  const isP1 = scope === 'phase1';
  const r = isP1 ? p1User(p1.role, p1.factory) : ROLES[role];
  const navItems = isP1 ? [
    { section: 'Configurations' },
    { id: 'library', label: 'Components', icon: 'category' },
    { id: 'processes', label: 'Processes', icon: 'precision_manufacturing' },
    { id: 'products', label: 'SKU', icon: 'inventory_2' },
    { section: 'Overview' },
    { id: 'component_stock', label: 'Component stock levels', icon: 'inventory' },
    { id: 'process_capacity', label: 'Process capacity', icon: 'speed' },
    { id: 'factory_share', label: 'Factory share', icon: 'tune' },
    { section: 'Balancer' },
    { id: 'routing', label: 'Routing dashboard', icon: 'alt_route' },
    { id: 'balancer_log', label: 'Balancer decision log', icon: 'manage_search' },
    { id: 'daily_plan', label: 'Daily plan', icon: 'event_note' },
    { id: 'transfers', label: 'Transfers', icon: 'swap_horiz' },
  ] : [
    { section: 'Catalog' },
    { id: 'design_groups', label: 'Design groups', icon: 'account_tree', count: (typeof DESIGN_GROUPS !== 'undefined' ? DESIGN_GROUPS.length : undefined) },
    { id: 'products', label: 'Products', icon: 'inventory_2', count: SKUS.length },
    { id: 'library',  label: 'Components', icon: 'category' },
    { id: 'processes', label: 'Processes', icon: 'precision_manufacturing' },
    { id: 'global_prices', label: 'Global prices', icon: 'currency_exchange' },
    { section: 'Workflow' },
    { id: 'queue',    label: 'My queue', icon: 'assignment_turned_in', count: countMine(role) },
    { section: 'Factories' },
    { id: 'sku_preferences', label: 'SKU preferences', icon: 'tune' },
    { id: 'component_stock', label: 'Component stock levels', icon: 'inventory' },
    { id: 'process_control', label: 'Process control', icon: 'fact_check' },
    { section: 'Operations' },
    { id: 'dashboard', label: 'Dashboard', icon: 'dashboard' },
    { id: 'performance', label: 'Performance view', icon: 'insights' },
    { id: 'audit',    label: 'Audit log', icon: 'history' },
    { id: 'reports',  label: 'Reports', icon: 'bar_chart' },
    { section: 'Setup' },
    { id: 'settings', label: 'Settings', icon: 'settings' },
  ];
  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: '#f4f6f8', fontFamily: 'Roboto, Helvetica, Arial, sans-serif', color: 'rgba(0,0,0,0.87)' }}>
      <aside style={{ width: 240, background: '#fff', borderRight: '1px solid rgba(0,0,0,0.12)', position: 'fixed', top: 0, left: 0, bottom: 0, paddingTop: 64, zIndex: 1 }}>
        <div style={{ position: 'absolute', top: 0, left: 0, right: 0, height: 64, display: 'flex', alignItems: 'center', padding: '0 16px', gap: 10, borderBottom: '1px solid rgba(0,0,0,0.12)' }}>
          <div style={{ width: 28, height: 28, borderRadius: 6, background: 'linear-gradient(135deg, #1976d2, #0288d1)', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color: '#fff' }}>
            <Icon name="diamond" size={18}/>
          </div>
          <div>
            <div style={{ fontWeight: 500, fontSize: 15, lineHeight: 1.1 }}>Factory Catalog</div>
            <div style={{ fontSize: 11, color: 'rgba(0,0,0,0.6)' }}>Tenengroup{isP1 ? ' · Phase 1' : ''}</div>
          </div>
        </div>
        <nav style={{ padding: '8px 0', overflowY: 'auto', height: 'calc(100vh - 64px)' }}>
          {navItems.map((it, i) => it.section ? (
            <div key={i} style={{ padding: '14px 16px 6px', fontSize: 11, letterSpacing: '0.08333em', textTransform: 'uppercase', color: 'rgba(0,0,0,0.6)' }}>{it.section}</div>
          ) : (
            <React.Fragment key={it.id}>
              <div onClick={() => onRoute(it.id)} style={{
                display: 'flex', alignItems: 'center', gap: 16, padding: '8px 16px', fontSize: 14, cursor: 'pointer',
                background: route === it.id ? 'rgba(25,118,210,0.08)' : 'transparent',
                color: route === it.id ? '#1976d2' : 'rgba(0,0,0,0.87)',
              }}>
                <Icon name={it.icon} size={20} sx={{ color: route === it.id ? '#1976d2' : 'rgba(0,0,0,0.54)' }}/>
                <span style={{ flex: 1 }}>{it.label}</span>
                {it.count !== undefined && it.count > 0 && (
                  <span style={{ fontSize: 11, padding: '1px 7px', borderRadius: 10, background: route === it.id ? '#1976d2' : 'rgba(0,0,0,0.08)', color: route === it.id ? '#fff' : 'rgba(0,0,0,0.6)', fontWeight: 500 }}>{it.count}</span>
                )}
              </div>
              {it.children && it.children.map(ch => (
                <div key={ch.id} onClick={() => onRoute(ch.id)} style={{
                  display: 'flex', alignItems: 'center', gap: 12, padding: '6px 16px 6px 44px', fontSize: 13, cursor: 'pointer',
                  background: route === ch.id ? 'rgba(25,118,210,0.08)' : 'transparent',
                  color: route === ch.id ? '#1976d2' : 'rgba(0,0,0,0.7)',
                }}>
                  <Icon name={ch.icon} size={16} sx={{ color: route === ch.id ? '#1976d2' : 'rgba(0,0,0,0.54)' }}/>
                  <span>{ch.label}</span>
                </div>
              ))}
            </React.Fragment>
          ))}
        </nav>
      </aside>

      <header style={{ position: 'fixed', top: 0, left: 240, right: 0, height: 64, background: '#fff', borderBottom: '1px solid rgba(0,0,0,0.12)', display: 'flex', alignItems: 'center', padding: '0 24px', gap: 16, zIndex: 1 }}>
        <h1 style={{ fontSize: 20, fontWeight: 500, margin: 0, flex: 1 }}>{isP1 && route === 'products' ? 'SKU' : labelOfRoute(route)}</h1>
        <div style={{ background: 'rgba(0,0,0,0.04)', borderRadius: 4, padding: '6px 12px', minWidth: 320, fontSize: 14, color: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', gap: 8 }}>
          <Icon name="search" size={20}/>
          <span>{isP1 ? 'Search SKUs, ERP IDs, orders…' : 'Search SKUs, TGI IDs, suppliers…'}</span>
          <span style={{ marginLeft: 'auto', fontSize: 11, padding: '1px 6px', border: '1px solid rgba(0,0,0,0.18)', borderRadius: 3 }}>⌘K</span>
        </div>
        {isP1 ? <P1RoleSwitcher p1={p1}/> : <RoleSwitcher role={role} onRole={onRole}/>}
        <IconButton name="notifications"/>
        <Avatar initials={r.initials} color={r.color}/>
      </header>

      <main style={{ marginLeft: 240, marginTop: 64, flex: 1, padding: 24, minHeight: 'calc(100vh - 64px)', boxSizing: 'border-box' }}>
        {children}
      </main>
    </div>
  );
}

function labelOfRoute(r) {
  return ({
    products: 'Products', design_groups: 'Design groups', queue: 'My queue', library: 'Components',
    processes: 'Processes', global_prices: 'Global prices',
    sku_preferences: 'SKU preferences', component_stock: 'Component stock levels', process_control: 'Process control',
    factory_share: 'Factory share', process_capacity: 'Process capacity', routing: 'Routing dashboard', balancer_log: 'Balancer decision log', daily_plan: 'Daily plan', transfers: 'Transfers',
    dashboard: 'Dashboard', performance: 'Performance view',
    audit: 'Audit log', reports: 'Reports', settings: 'Settings',
  })[r] || 'Factory Catalog';
}

function countMine(role) {
  return SKUS.filter(sku => {
    const draft = sku.versions.find(v => v.draft);
    if (!draft) return false;
    if (role === 'merchandiser') return draft.status === 'draft';
    if (role === 'supervisor') return draft.status === 'prod_review';
    if (role === 'catalog_manager') return draft.status === 'pending';
    return false;
  }).length;
}

function RoleSwitcher({ role, onRole }) {
  const [open, setOpen] = useStateApp(false);
  const r = ROLES[role];
  return (
    <div style={{ position: 'relative' }}>
      <button onClick={() => setOpen(!open)} style={{
        display: 'inline-flex', alignItems: 'center', gap: 8, padding: '6px 10px 6px 6px',
        borderRadius: 20, border: '1px solid rgba(0,0,0,0.12)', background: '#fff', cursor: 'pointer',
        fontFamily: 'Roboto', fontSize: 13,
      }}>
        <span style={{ width: 24, height: 24, borderRadius: '50%', background: r.color, color: '#fff', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}>
          <Icon name={r.icon} size={14}/>
        </span>
        <span style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', lineHeight: 1.2 }}>
          <span style={{ fontSize: 11, color: 'rgba(0,0,0,0.6)' }}>Acting as</span>
          <span style={{ fontWeight: 500 }}>{r.label}</span>
        </span>
        <Icon name="expand_more" size={18}/>
      </button>
      {open && (
        <>
          <div onClick={() => setOpen(false)} style={{ position: 'fixed', inset: 0, zIndex: 10 }}/>
          <div style={{ position: 'absolute', right: 0, top: '110%', minWidth: 280, background: '#fff', borderRadius: 8, boxShadow: '0px 5px 5px -3px rgba(0,0,0,0.2),0px 8px 10px 1px rgba(0,0,0,0.14),0px 3px 14px 2px rgba(0,0,0,0.12)', zIndex: 11, padding: 4 }}>
            <div style={{ padding: '8px 12px', fontSize: 11, letterSpacing: '0.08333em', textTransform: 'uppercase', color: 'rgba(0,0,0,0.6)' }}>Switch persona (demo)</div>
            {Object.values(ROLES).filter(opt => opt.level).map(opt => (
              <div key={opt.id} onClick={() => { onRole(opt.id); setOpen(false); }} style={{
                display: 'flex', alignItems: 'center', gap: 12, padding: '8px 12px', borderRadius: 4,
                cursor: 'pointer', background: role === opt.id ? 'rgba(25,118,210,0.08)' : 'transparent',
              }}
              onMouseEnter={e => { if (role !== opt.id) e.currentTarget.style.background = 'rgba(0,0,0,0.04)'; }}
              onMouseLeave={e => { if (role !== opt.id) e.currentTarget.style.background = 'transparent'; }}>
                <span style={{ width: 32, height: 32, borderRadius: '50%', background: opt.color, color: '#fff', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Icon name={opt.icon} size={18}/>
                </span>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 14, fontWeight: 500 }}>{opt.label}</div>
                  <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>{opt.name}</div>
                </div>
                {role === opt.id && <Icon name="check" size={18} sx={{ color: '#1976d2' }}/>}
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

function P1RoleSwitcher({ p1 }) {
  const [open, setOpen] = useStateApp(false);
  const u = p1User(p1.role, p1.factory);
  return (
    <div style={{ position: 'relative' }}>
      <button onClick={() => setOpen(!open)} style={{ display: 'inline-flex', alignItems: 'center', gap: 8, padding: '6px 10px 6px 6px', borderRadius: 20, border: '1px solid rgba(0,0,0,0.12)', background: '#fff', cursor: 'pointer', fontFamily: 'Roboto', fontSize: 13 }}>
        <span style={{ width: 24, height: 24, borderRadius: '50%', background: u.color, color: '#fff', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}><Icon name={u.icon} size={14}/></span>
        <span style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', lineHeight: 1.2 }}><span style={{ fontSize: 11, color: 'rgba(0,0,0,0.6)' }}>Acting as</span><span style={{ fontWeight: 500 }}>{u.label}</span></span>
        <Icon name="expand_more" size={18}/>
      </button>
      {open && (<>
        <div onClick={() => setOpen(false)} style={{ position: 'fixed', inset: 0, zIndex: 10 }}></div>
        <div style={{ position: 'absolute', right: 0, top: '110%', minWidth: 300, background: '#fff', borderRadius: 8, boxShadow: '0px 5px 5px -3px rgba(0,0,0,0.2),0px 8px 10px 1px rgba(0,0,0,0.14),0px 3px 14px 2px rgba(0,0,0,0.12)', zIndex: 11, padding: 4 }}>
          <div style={{ padding: '8px 12px', fontSize: 11, letterSpacing: '0.08333em', textTransform: 'uppercase', color: 'rgba(0,0,0,0.6)' }}>Switch persona (demo)</div>
          {Object.values(P1_ROLES).map(opt => { const on = p1.role === opt.id; const ou = p1User(opt.id, p1.factory); return (
            <div key={opt.id} style={{ borderRadius: 4, background: on ? 'rgba(25,118,210,0.08)' : 'transparent' }}>
              <div onClick={() => { p1.onRole(opt.id); if (opt.id !== 'factory_manager') setOpen(false); }} style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '8px 12px', cursor: 'pointer' }}>
                <span style={{ width: 32, height: 32, borderRadius: '50%', background: opt.color, color: '#fff', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}><Icon name={opt.icon} size={18}/></span>
                <div style={{ flex: 1 }}><div style={{ fontSize: 14, fontWeight: 500 }}>{opt.label}</div><div style={{ fontSize: 12, color: 'rgba(0,0,0,0.6)' }}>{ou.name}{opt.viewOnly ? ' · view only' : ''}</div></div>
                {on && <Icon name="check" size={18} sx={{ color: '#1976d2' }}/>}
              </div>
              {opt.id === 'factory_manager' && <div style={{ display: 'flex', gap: 6, padding: '0 12px 10px 56px' }}>
                {P1_F.map(f => { const sel = on && p1.factory === f; return <button key={f} onClick={() => { p1.onRole('factory_manager'); p1.onFactory(f); setOpen(false); }} style={{ border: '1px solid ' + (sel ? '#1976d2' : 'rgba(0,0,0,0.2)'), background: sel ? '#1976d2' : '#fff', color: sel ? '#fff' : 'rgba(0,0,0,0.75)', borderRadius: 4, padding: '3px 10px', fontSize: 12, fontWeight: 600, cursor: 'pointer', fontFamily: 'Roboto' }}>{f}</button>; })}
              </div>}
            </div>); })}
        </div>
      </>)}
    </div>
  );
}

Object.assign(window, { CatalogShell, RoleSwitcher, P1RoleSwitcher });

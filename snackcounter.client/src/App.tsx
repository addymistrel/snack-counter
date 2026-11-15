import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import './App.css';

interface Office { id: string; name: string; currency: string; monthlyBudget: number; }
interface Ticket { id: string; containerId: string; officeName: string; currency: string; status: string; reservedBudget: number; spentAmount?: number; }
interface Dashboard { offices: Office[]; tickets: Ticket[]; }
interface SnackSummary { name: string; quantity: number; spend: number; currency: string; }
interface MonthlyReport { month: string; consolidatedNokTotal: number; topSnacks: SnackSummary[]; }

function App() {
    const [dashboard, setDashboard] = useState<Dashboard>();
    const [monthlyReport, setMonthlyReport] = useState<MonthlyReport>();
    const [containerId, setContainerId] = useState(getQrContainerId);
    const [officeId, setOfficeId] = useState('oslo');
    const [volunteerId, setVolunteerId] = useState('volunteer-1');
    const [notice, setNotice] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const qrContainerId = getQrContainerId();

    useEffect(() => {
        void refresh();
    }, []);

    return (
        <main>
            <header className="hero">
                <div><p className="eyebrow">Snack Counter / operations</p><h1>Keep the good stuff<br /><span>within reach.</span></h1><p className="lede">One clear handoff from empty container to reimbursed volunteer.</p></div>
                <div className="pulse"><strong>{dashboard?.tickets.length ?? 0}</strong><span>tickets this month</span></div>
            </header>
            <section className="grid">
                <form className="card report-card" onSubmit={reportContainer}>
                    <div className="card-heading"><span className="step">01</span><div><h2>{qrContainerId ? 'Confirm this empty' : 'Report an empty'}</h2><p>{qrContainerId ? `QR link for ${qrContainerId}. Confirm the details to continue.` : 'Start a replenishment ticket in three quick confirmations.'}</p></div></div>
                    <label>Container ID<input data-testid="container-id" value={containerId} onChange={event => setContainerId(event.target.value)} placeholder="e.g. pantry-oslo-04" required /></label>
                    <label>Office<select value={officeId} onChange={event => setOfficeId(event.target.value)}>{dashboard?.offices.map(office => <option key={office.id} value={office.id}>{office.name} · {office.currency}</option>)}</select></label>
                    <div className="confirmations"><label><input type="checkbox" required /> Empty</label><label><input type="checkbox" required /> Location</label><label><input type="checkbox" required /> Snack type</label></div>
                    <button disabled={isSubmitting}>{isSubmitting ? 'Creating…' : 'Create ticket →'}</button>
                </form>
                <section className="card insight-card">
                    <div className="card-heading"><span className="step">02</span><div><h2>Volunteer inbox</h2><p>Claim the next open ticket before the deadline.</p></div></div>
                    <label>Your volunteer ID<input value={volunteerId} onChange={event => setVolunteerId(event.target.value)} /></label>
                    <div className="ticket-list">{dashboard?.tickets.filter(ticket => ticket.status === 'Open').slice(0, 3).map(ticket => <div className="ticket" key={ticket.id}><div><strong>{ticket.containerId}</strong><span>{ticket.officeName} · {ticket.reservedBudget} {ticket.currency}</span></div><button className="secondary" onClick={() => acknowledge(ticket.id)}>Acknowledge</button></div>)}</div>
                    {!dashboard?.tickets.some(ticket => ticket.status === 'Open') && <p className="muted">No open tickets right now.</p>}
                </section>
            </section>
            <section className="card table-card">
                <div className="card-heading"><span className="step">03</span><div><h2>Monthly control room</h2><p>Native currency budgets with a single NOK view for management.</p></div><button className="secondary" onClick={() => void refresh()}>Refresh</button></div>
                <div className="office-grid">{dashboard?.offices.map(office => { const tickets = dashboard.tickets.filter(ticket => ticket.officeName === office.name); const spent = tickets.reduce((sum, ticket) => sum + (ticket.spentAmount ?? 0), 0); return <div className="office" key={office.id}><span>{office.name}</span><strong>{spent.toLocaleString()} {office.currency}</strong><small>{tickets.length} tickets · budget {office.monthlyBudget.toLocaleString()}</small><div className="bar"><i style={{ width: `${Math.min(100, (spent / office.monthlyBudget) * 100)}%` }} /></div></div>; })}</div>
                <div className="report-summary">
                    <div><span>Consolidated NOK</span><strong>{monthlyReport?.consolidatedNokTotal.toLocaleString() ?? '0'} NOK</strong><small>{monthlyReport?.month ?? 'Current month'} · sample rate</small></div>
                    <div><span>Top snacks</span>{monthlyReport?.topSnacks.length ? monthlyReport.topSnacks.map(snack => <small key={`${snack.name}-${snack.currency}`}>{snack.name} · {snack.quantity} · {snack.spend.toLocaleString()} {snack.currency}</small>) : <small>No purchases yet.</small>}</div>
                </div>
            </section>
            {notice && <p className="notice" role="status">{notice}</p>}
        </main>
    );

    async function refresh() {
        const response = await fetch('/api/snack-counter/dashboard');
        if (response.ok) setDashboard(await response.json() as Dashboard);
        const reportResponse = await fetch('/api/snack-counter/reports/monthly');
        if (reportResponse.ok) setMonthlyReport(await reportResponse.json() as MonthlyReport);
    }

    async function reportContainer(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setIsSubmitting(true);
        const response = await fetch('/api/snack-counter/containers/report', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ containerId, officeId, containerEmpty: true, locationConfirmed: true, snackTypeConfirmed: true }) });
        const result = await response.json() as { error?: string };
        setNotice(response.ok ? 'Ticket created. Volunteers have been notified.' : result.error ?? 'Unable to create ticket.');
        if (response.ok) setContainerId('');
        await refresh();
        setIsSubmitting(false);
    }

    async function acknowledge(ticketId: string) {
        const response = await fetch(`/api/snack-counter/tickets/${ticketId}/acknowledge`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ volunteerId, reimbursementConsent: true }) });
        const result = await response.json() as { error?: string };
        setNotice(response.ok ? 'Ticket acknowledged. Submit proof after shopping.' : result.error ?? 'Unable to acknowledge ticket.');
        await refresh();
    }

    function getQrContainerId() {
        const match = window.location.pathname.match(/^\/r\/([^/]+)$/);
        if (!match) return '';
        return decodeURIComponent(match[1]);
    }
}

export default App;

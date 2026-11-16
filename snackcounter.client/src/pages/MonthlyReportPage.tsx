import type { Dashboard, MonthlyReport } from '../api/client';

interface MonthlyReportPageProps {
    dashboard: Dashboard;
    report?: MonthlyReport;
    onRefresh: () => Promise<void>;
}

export function MonthlyReportPage({ dashboard, report, onRefresh }: MonthlyReportPageProps) {
    const lockedFx = report?.lockedExchangeRateInrToNok ?? 0.13;
    const summaries = report?.offices ?? dashboard.offices.map(office => ({
        officeId: office.id,
        officeName: office.name,
        currency: office.currency,
        budget: office.monthlyBudget,
        reserved: 0,
        spent: 0,
        openTickets: 0,
        closedTickets: 0,
    }));
    const consolidatedNok = report?.consolidatedNokTotal
        ?? summaries.reduce((total, office) => total + office.spent * (office.currency === 'INR' ? lockedFx : 1), 0);

    return (
        <div className="card">
            <div className="card-title" style={{ justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <span className="step-badge">Step 11</span>
                    <div>
                        <h2>Monthly Management Spending Control Room</h2>
                        <p style={{ margin: 0, fontSize: '12px', color: 'var(--text-muted)' }}>
                            Native currency budgets per office, consolidated single NOK total for management, and snack consumption analytics.
                        </p>
                    </div>
                </div>
                <button type="button" className="btn-sm btn-secondary" onClick={() => void onRefresh()}>
                    🔄 Refresh Report
                </button>
            </div>

            {/* Office Budget Grid */}
            <div className="grid-2" style={{ marginBottom: '24px' }}>
                {summaries.map(office => {
                    const percentage = Math.min(100, (office.spent / office.budget) * 100);
                    return <div key={office.officeId} style={{ background: 'var(--surface-alt)', border: '1px solid var(--border)', borderRadius: '12px', padding: '18px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <div>
                            <span style={{ fontSize: '12px', fontWeight: 700, color: 'var(--text-muted)', textTransform: 'uppercase' }}>
                                {office.officeName} Office
                            </span>
                            <div style={{ fontSize: '28px', fontWeight: 800, margin: '4px 0' }}>
                                {office.spent.toLocaleString()} <span style={{ fontSize: '16px', fontWeight: 600 }}>{office.currency}</span>
                            </div>
                        </div>
                        <span className="tag tag-ack">Budget: {office.budget.toLocaleString()} {office.currency}</span>
                    </div>

                    <div style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'flex', justifyContent: 'space-between', marginTop: '6px' }}>
                        <span>Reserved: {office.reserved.toLocaleString()} {office.currency}</span>
                        <span>Remaining: {Math.max(0, office.budget - office.spent - office.reserved).toLocaleString()} {office.currency}</span>
                    </div>

                    <div className="budget-bar-bg">
                        <div
                            className={`budget-bar-fill ${percentage > 90 ? 'danger' : ''}`}
                            style={{ width: `${percentage}%` }}
                        />
                    </div>
                    <div style={{ textAlign: 'right', fontSize: '10px', color: 'var(--text-muted)', marginTop: '4px' }}>
                        {percentage.toFixed(1)}% utilized
                    </div>
                </div>;
                })}
            </div>

            {/* Consolidated Management Summary Card */}
            <div style={{ background: 'var(--text-main)', color: '#ffffff', borderRadius: '12px', padding: '20px', marginBottom: '24px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '16px' }}>
                    <div>
                        <span style={{ fontSize: '11px', fontWeight: 800, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#a0b6a9' }}>
                            Executive Consolidated Total
                        </span>
                        <div style={{ fontSize: '36px', fontWeight: 800, letterSpacing: '-0.04em', margin: '4px 0' }}>
                            {consolidatedNok.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} NOK
                        </div>
                        <div style={{ fontSize: '12px', color: '#b8cbbe' }}>
                            Consolidates Oslo spending + Bengaluru spending converted at the locked rate (1 INR = {lockedFx} NOK).
                        </div>
                    </div>

                    <div style={{ background: 'rgba(255,255,255,0.08)', padding: '12px 16px', borderRadius: '8px', fontSize: '11px', lineHeight: '1.6' }}>
                        <div><strong>Month:</strong> {report?.month ?? new Date().toISOString().slice(0, 7)}</div>
                        {summaries.map(office => (
                            <div key={office.officeId}>
                                <strong>{office.officeName}:</strong> {office.spent.toLocaleString()} {office.currency}
                                {office.currency === 'INR' && ` (${(office.spent * lockedFx).toFixed(1)} NOK eq.)`}
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Top Consumed Snacks Analytics */}
            <div>
                <h3 style={{ fontSize: '15px', marginBottom: '12px' }}>
                    Top Snacks Eaten This Month
                </h3>
                {report?.topSnacks && report.topSnacks.length > 0 ? (
                    <table>
                        <thead>
                            <tr>
                                <th>Rank</th>
                                <th>Snack Item</th>
                                <th>Quantity Consumed</th>
                                <th>Total Spend</th>
                                <th>Currency</th>
                            </tr>
                        </thead>
                        <tbody>
                            {report.topSnacks.map((s, idx) => (
                                <tr key={`${s.name}-${s.currency}`}>
                                    <td><strong>#{idx + 1}</strong></td>
                                    <td><strong>{s.name}</strong></td>
                                    <td>{s.quantity} units</td>
                                    <td>{s.spend.toLocaleString()}</td>
                                    <td><span className="tag tag-open">{s.currency}</span></td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                ) : (
                    <div style={{ padding: '16px', textAlign: 'center', background: '#fafafa', borderRadius: '8px', fontSize: '12px', color: 'var(--text-muted)' }}>
                        No snack purchases recorded for this period yet. Complete a refill to see consumption metrics!
                    </div>
                )}
            </div>
        </div>
    );
}

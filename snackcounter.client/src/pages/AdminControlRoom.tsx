import { useState } from 'react';
import {
    getApiError,
    approveBudget,
    toggleHRDown,
    retryPendingClaims,
    runNightlySync,
    type Claim,
    type Ticket,
    type Office
} from '../api/client';

interface AdminControlRoomProps {
    tickets: Ticket[];
    claims: Claim[];
    offices: Office[];
    isHRDown: boolean;
    onCompleted: (message: string) => Promise<void>;
}

export function AdminControlRoom({
    tickets,
    claims,
    offices,
    isHRDown,
    onCompleted
}: AdminControlRoomProps) {
    const [isActionInProgress, setIsActionInProgress] = useState(false);
    const [error, setError] = useState('');

    const awaitingBudgetTickets = tickets.filter(t => t.status === 'AwaitingBudgetApproval');
    const exceptionClaims = claims.filter(c => c.isException);
    const pendingRetryClaims = claims.filter(c => c.status === 'PendingRetry');

    async function handleBudgetDecision(ticketId: string, approved: boolean) {
        setIsActionInProgress(true);
        setError('');
        try {
            await approveBudget(ticketId, approved, approved ? 'Approved by Admin' : 'Declined: monthly budget limit reached');
            await onCompleted(approved ? 'Ticket budget approved and opened for volunteers.' : 'Ticket declined and cancelled.');
        } catch (err) {
            setError(getApiError(err, 'Budget decision failed.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    async function handleToggleHR() {
        setIsActionInProgress(true);
        setError('');
        try {
            const res = await toggleHRDown();
            await onCompleted(`HR System simulator is now ${res.isDown ? 'OFFLINE (claims will queue as PendingRetry)' : 'ONLINE (operational)'}.`);
        } catch (err) {
            setError(getApiError(err, 'Failed to toggle HR state.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    async function handleRetryClaims() {
        setIsActionInProgress(true);
        setError('');
        try {
            const res = await retryPendingClaims();
            await onCompleted(`Retried ${res.retriedCount} pending claim(s) using original deterministic reference IDs. No duplicates created.`);
        } catch (err) {
            setError(getApiError(err, 'Retry failed.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    async function handleNightlySync() {
        setIsActionInProgress(true);
        setError('');
        try {
            const res = await runNightlySync();
            await onCompleted(`Nightly HR batch sync completed! ${res.processedCount} claim(s) reimbursed & tickets closed.`);
        } catch (err) {
            setError(getApiError(err, 'Nightly sync failed.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    return (
        <div style={{ display: 'grid', gap: '24px' }}>
            {/* SECTION 1: BUDGET APPROVAL GATE (STEP 3) */}
            <div className="card">
                <div className="card-title">
                    <span className="step-badge">Step 3 Gate</span>
                    <div>
                        <h2>Over-Budget Tickets Awaiting Admin Approval ({awaitingBudgetTickets.length})</h2>
                        <p style={{ margin: 0, fontSize: '12px', color: 'var(--text-muted)' }}>
                            When an estimated refill exceeds remaining monthly office budget, it waits for admin approval before notifying volunteers.
                        </p>
                    </div>
                </div>

                {awaitingBudgetTickets.length === 0 ? (
                    <div style={{ padding: '14px', background: '#fafafa', borderRadius: '8px', textAlign: 'center', fontSize: '12px', color: 'var(--text-muted)' }}>
                        All current tickets are within budget. No tickets waiting for approval.
                    </div>
                ) : (
                    awaitingBudgetTickets.map(t => {
                        const office = offices.find(o => o.id === t.officeId);
                        return (
                            <div key={t.id} className="ticket-card" style={{ borderColor: 'var(--warning)' }}>
                                <div className="ticket-header">
                                    <div>
                                        <div className="ticket-title">{t.snackName} ({t.quantity} units)</div>
                                        <div className="ticket-meta">{t.counterName} · {t.officeName} ({t.containerId})</div>
                                    </div>
                                    <span className="tag tag-warning">Estimated: {t.quantity * t.unitPrice} {t.currency}</span>
                                </div>

                                <div style={{ fontSize: '11px', color: 'var(--text-muted)', margin: '4px 0' }}>
                                    Office Monthly Budget: {office?.monthlyBudget.toLocaleString()} {t.currency}. This purchase exceeds remaining room.
                                </div>

                                <div style={{ display: 'flex', gap: '8px', marginTop: '8px' }}>
                                    <button
                                        type="button"
                                        className="btn-sm btn-primary"
                                        disabled={isActionInProgress}
                                        onClick={() => void handleBudgetDecision(t.id, true)}
                                    >
                                        ✓ Approve Budget Override
                                    </button>
                                    <button
                                        type="button"
                                        className="btn-sm btn-danger"
                                        disabled={isActionInProgress}
                                        onClick={() => void handleBudgetDecision(t.id, false)}
                                    >
                                        ✕ Decline Refill
                                    </button>
                                </div>
                            </div>
                        );
                    })
                )}
            </div>

            {/* SECTION 2: SIMULATED HR SYSTEM & REIMBURSEMENT (STEP 9 & 10) */}
            <div className="card">
                <div className="card-title" style={{ justifyContent: 'space-between' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                        <span className="step-badge">Step 9 & 10</span>
                        <div>
                            <h2>HR Reimbursement Stand-In System</h2>
                            <p style={{ margin: 0, fontSize: '12px', color: 'var(--text-muted)' }}>
                                Simulates external HR API, claim creation, network downtime, idempotent retries, and nightly batch payment sync.
                            </p>
                        </div>
                    </div>

                    <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                        <span className={`badge ${isHRDown ? 'badge-offline' : 'badge-online'}`}>
                            {isHRDown ? 'HR System: DOWN' : 'HR System: ONLINE'}
                        </span>
                    </div>
                </div>

                {/* Simulation Control Toolbar */}
                <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', padding: '12px', background: 'var(--surface-alt)', borderRadius: '8px', marginBottom: '16px' }}>
                    <button
                        type="button"
                        className={`btn-sm ${isHRDown ? 'btn-primary' : 'btn-secondary'}`}
                        onClick={() => void handleToggleHR()}
                        disabled={isActionInProgress}
                    >
                        {isHRDown ? '🟢 Bring HR Back Online' : '🔴 Simulate HR Downtime'}
                    </button>

                    <button
                        type="button"
                        className="btn-sm btn-secondary"
                        onClick={() => void handleRetryClaims()}
                        disabled={isActionInProgress || pendingRetryClaims.length === 0 || isHRDown}
                        title="Retry claims queued during downtime using deterministic IDs"
                    >
                        🔄 Retry Pending Claims ({pendingRetryClaims.length})
                    </button>

                    <button
                        type="button"
                        className="btn-sm btn-primary"
                        onClick={() => void handleNightlySync()}
                        disabled={isActionInProgress}
                        title="Simulate nightly batch job: fetches HR approvals, pays claims, closes tickets"
                    >
                        🌙 Run Nightly Batch Sync
                    </button>
                </div>

                {/* Pending Retry Warning */}
                {pendingRetryClaims.length > 0 && (
                    <div style={{ background: '#fff3cd', color: '#856404', padding: '10px 14px', borderRadius: '8px', fontSize: '12px', marginBottom: '16px' }}>
                        ⚠️ <strong>{pendingRetryClaims.length} claim(s) queued as PendingRetry.</strong> When HR is back online, click "Retry Pending Claims" to submit with identical reference IDs.
                    </div>
                )}

                {/* HR Claims Table */}
                <div style={{ overflowX: 'auto' }}>
                    <table>
                        <thead>
                            <tr>
                                <th>Ref ID</th>
                                <th>Volunteer / Claimant</th>
                                <th>Amount</th>
                                <th>Status</th>
                                <th>Type</th>
                                <th>Submitted At</th>
                            </tr>
                        </thead>
                        <tbody>
                            {claims.length === 0 ? (
                                <tr>
                                    <td colSpan={6} style={{ textAlign: 'center', padding: '20px', color: 'var(--text-muted)' }}>
                                        No reimbursement claims submitted yet.
                                    </td>
                                </tr>
                            ) : (
                                claims.map(c => (
                                    <tr key={c.id}>
                                        <td><code>{c.referenceId}</code></td>
                                        <td><strong>{c.volunteerName}</strong></td>
                                        <td>{c.amount.toLocaleString()} {c.currency}</td>
                                        <td>
                                            <span className={`tag ${
                                                c.status === 'Paid' ? 'tag-closed' :
                                                c.status === 'PendingRetry' ? 'tag-warning' :
                                                c.status === 'Submitted' ? 'tag-submitted' : 'tag-ack'
                                            }`}>
                                                {c.status}
                                            </span>
                                        </td>
                                        <td>
                                            {c.isException ? (
                                                <span className="tag tag-warning">Side-Path Exception</span>
                                            ) : (
                                                <span className="tag tag-open">Standard Refill</span>
                                            )}
                                        </td>
                                        <td style={{ color: 'var(--text-muted)' }}>
                                            {new Date(c.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>

                {error && <p style={{ color: 'var(--danger)', fontSize: '12px', marginTop: '12px' }}>{error}</p>}
            </div>

            {/* SECTION 3: SIDE PATH EXCEPTIONS */}
            {exceptionClaims.length > 0 && (
                <div className="card">
                    <div className="card-title">
                        <span className="step-badge">Side Path</span>
                        <div>
                            <h2>"Already Refilled" Exception Claims ({exceptionClaims.length})</h2>
                            <p style={{ margin: 0, fontSize: '12px', color: 'var(--text-muted)' }}>
                                Claims submitted by staff who restocked containers without a ticket. Credited to the buyer, never the assigned volunteer.
                            </p>
                        </div>
                    </div>

                    <div style={{ display: 'grid', gap: '10px' }}>
                        {exceptionClaims.map(c => (
                            <div key={c.id} className="ticket-card" style={{ borderColor: 'var(--primary)' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                    <div>
                                        <strong>{c.exceptionReporterName}</strong> refilled informally
                                        <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                                            Claim Ref: <code>{c.referenceId}</code>
                                        </div>
                                    </div>
                                    <span className="tag tag-closed">{c.amount.toLocaleString()} {c.currency}</span>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

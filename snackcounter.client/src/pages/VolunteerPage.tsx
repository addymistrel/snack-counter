import { useState } from 'react';
import type { FormEvent } from 'react';
import {
    getApiError,
    acknowledgeTicket,
    transferTicket,
    simulateAutoPick,
    simulateExpiry,
    submitProof,
    getClaim,
    type Claim,
    type Ticket,
    type Volunteer
} from '../api/client';

interface VolunteerPageProps {
    tickets: Ticket[];
    volunteers: Volunteer[];
    currentVolunteer: Volunteer;
    onCompleted: (message: string, claim?: Claim) => Promise<void>;
}

export function VolunteerPage({
    tickets,
    volunteers,
    currentVolunteer,
    onCompleted
}: VolunteerPageProps) {
    const [isActionInProgress, setIsActionInProgress] = useState(false);
    const [error, setError] = useState('');

    // Debt Protection Consent Modal State
    const [debtModalTicketId, setDebtModalTicketId] = useState<string | null>(null);

    // Transfer Modal State
    const [transferTicketId, setTransferTicketId] = useState<string | null>(null);
    const [transferTargetId, setTransferTargetId] = useState('');

    // Proof Form State
    const [activeProofTicketId, setActiveProofTicketId] = useState<string | null>(null);
    const [proofAmount, setProofAmount] = useState('');
    const [proofItems, setProofItems] = useState('Salted Roasted Almonds x10');
    const [proofReceipt, setProofReceipt] = useState('receipt-oslo-01.jpg');
    const [proofPhoto, setProofPhoto] = useState('counter-restocked.jpg');

    // Filter tickets
    const openTickets = tickets.filter(t => t.status === 'Open');
    const myAssignedTickets = tickets.filter(t => t.status === 'Acknowledged' && t.volunteerId === currentVolunteer.id);
    const otherAssignedTickets = tickets.filter(t => t.status === 'Acknowledged' && t.volunteerId !== currentVolunteer.id);

    // Eligible volunteers for transfer (same office, active, not on leave, < 2 open tickets, not self)
    const transferCandidates = volunteers.filter(v =>
        v.currentOfficeId === currentVolunteer.currentOfficeId &&
        v.id !== currentVolunteer.id &&
        v.isActive &&
        !v.isOnLeave &&
        v.openTicketsCount < 2
    );

    async function handleAcknowledge(ticketId: string, consent = false) {
        setError('');
        setIsActionInProgress(true);
        try {
            await acknowledgeTicket(ticketId, currentVolunteer.id, consent);
            setDebtModalTicketId(null);
            await onCompleted(`Ticket accepted! You are now the refill owner. You have 48 hours to buy snacks and upload proof.`);
        } catch (err) {
            const apiErr = getApiError(err, 'Unable to accept ticket.');
            if (apiErr.includes('Reimbursement consent is required')) {
                // Open debt protection modal
                setDebtModalTicketId(ticketId);
            } else {
                setError(apiErr);
            }
        } finally {
            setIsActionInProgress(false);
        }
    }

    async function handleTransferSubmit(event: FormEvent) {
        event.preventDefault();
        if (!transferTicketId || !transferTargetId) return;

        setIsActionInProgress(true);
        setError('');
        try {
            const target = volunteers.find(v => v.id === transferTargetId);
            const needsConsent = (target?.unpaidClaimsCount ?? 0) >= 2;

            await transferTicket(transferTicketId, currentVolunteer.id, transferTargetId, needsConsent);
            setTransferTicketId(null);
            await onCompleted(`Ticket successfully transferred to ${target?.name ?? transferTargetId}. Completion deadline remains unchanged.`);
        } catch (err) {
            setError(getApiError(err, 'Unable to transfer ticket.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    async function handleSimulateAutoPick(ticketId: string) {
        setError('');
        setIsActionInProgress(true);
        try {
            const ticket = await simulateAutoPick(ticketId);
            await onCompleted(`4-hour timer expired: Auto-pick selected volunteer ${ticket.volunteerName} based on fewest open tickets & longest since last assignment.`);
        } catch (err) {
            setError(getApiError(err, 'Auto-pick simulation failed.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    async function handleSimulateExpiry(ticketId: string) {
        setError('');
        setIsActionInProgress(true);
        try {
            const ticket = await simulateExpiry(ticketId);
            await onCompleted(`48-hour deadline passed without proof! Assignment for previous volunteer expired. Reassigned to ${ticket.volunteerName} with a fresh 48-hour deadline.`);
        } catch (err) {
            setError(getApiError(err, 'Deadline expiry simulation failed.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    async function handleProofSubmit(event: FormEvent, ticket: Ticket) {
        event.preventDefault();
        setError('');
        setIsActionInProgress(true);
        try {
            const amountNum = parseFloat(proofAmount);
            await submitProof(ticket.id, {
                volunteerId: currentVolunteer.id,
                amount: amountNum,
                items: proofItems.split(',').map(s => s.trim()),
                receiptFileName: proofReceipt,
                counterPhotoFileName: proofPhoto
            });

            const claim = await getClaim(ticket.id);
            setActiveProofTicketId(null);
            setProofAmount('');

            const excessNote = amountNum > ticket.reservedBudget
                ? ` Actual amount (${amountNum} ${ticket.currency}) exceeds estimated reserve (${ticket.reservedBudget} ${ticket.currency}) — flagged as ProofExceedsEstimate but processed!`
                : '';

            await onCompleted(`Proof submitted for ${ticket.snackName}.${excessNote}`, claim);
        } catch (err) {
            setError(getApiError(err, 'Unable to submit proof.'));
        } finally {
            setIsActionInProgress(false);
        }
    }

    return (
        <div className="card">
            <div className="card-title">
                <span className="step-badge">Step 5, 6, 7 & 8</span>
                <div>
                    <h2>Volunteer Refill Hub</h2>
                    <p style={{ margin: 0, fontSize: '12px', color: 'var(--text-muted)' }}>
                        Claim open requests, protect your finances, transfer or submit proof.
                    </p>
                </div>
            </div>

            {/* Current Volunteer Status Card */}
            <div style={{ background: 'var(--surface-alt)', border: '1px solid var(--border)', borderRadius: '10px', padding: '12px 14px', marginBottom: '18px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                        <strong style={{ fontSize: '13px' }}>{currentVolunteer.name}</strong>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)', marginLeft: '8px' }}>
                            {currentVolunteer.currentOfficeId.toUpperCase()} (Home: {currentVolunteer.homeOfficeId.toUpperCase()}) · {currentVolunteer.payrollCurrency}
                        </span>
                    </div>
                    <div style={{ display: 'flex', gap: '8px' }}>
                        <span className={`badge ${currentVolunteer.openTicketsCount >= 2 ? 'badge-offline' : 'badge-online'}`}>
                            {currentVolunteer.openTicketsCount} / 2 Open Tickets
                        </span>
                        <span className={`badge ${currentVolunteer.unpaidClaimsCount >= 2 ? 'badge-warning' : 'badge-online'}`}>
                            {currentVolunteer.unpaidClaimsCount} Unpaid Claims
                        </span>
                    </div>
                </div>

                {currentVolunteer.unpaidClaimsCount >= 2 && (
                    <div style={{ marginTop: '8px', fontSize: '11px', color: '#856404', display: 'flex', alignItems: 'center', gap: '6px' }}>
                        ⚠️ <strong>Debt Protection Active:</strong> You have {currentVolunteer.unpaidClaimsCount} unpaid claims pending reimbursement. You must consent before accepting another ticket.
                    </div>
                )}
            </div>

            {/* SECTION: OPEN TICKETS (AWAITING VOLUNTEER) */}
            <div style={{ marginBottom: '24px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                    <h3 style={{ margin: 0, fontSize: '14px', letterSpacing: '-0.01em' }}>
                        Open Refill Requests ({openTickets.length})
                    </h3>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                        First to accept gets ownership · 4h auto-pick
                    </span>
                </div>

                {openTickets.length === 0 ? (
                    <div style={{ padding: '16px', background: '#fafafa', borderRadius: '8px', textAlign: 'center', fontSize: '12px', color: 'var(--text-muted)' }}>
                        No open tickets in need of volunteers right now.
                    </div>
                ) : (
                    openTickets.map(t => (
                        <div key={t.id} className="ticket-card">
                            <div className="ticket-header">
                                <div>
                                    <div className="ticket-title">{t.snackName} ({t.quantity} units)</div>
                                    <div className="ticket-meta">{t.counterName} · {t.officeName} ({t.containerId})</div>
                                </div>
                                <span className="tag tag-open">Budget: {t.reservedBudget} {t.currency}</span>
                            </div>

                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '6px', fontSize: '11px' }}>
                                <span style={{ color: 'var(--text-muted)' }}>
                                    Reported by {t.reporterName || 'Employee'}
                                </span>
                                <div style={{ display: 'flex', gap: '8px' }}>
                                    <button
                                        type="button"
                                        className="btn-sm btn-primary"
                                        disabled={isActionInProgress}
                                        onClick={() => void handleAcknowledge(t.id, false)}
                                    >
                                        Accept Refill →
                                    </button>
                                    <button
                                        type="button"
                                        className="btn-sm btn-secondary"
                                        title="Fast-forward 4 hours to test auto-pick rotation algorithm"
                                        disabled={isActionInProgress}
                                        onClick={() => void handleSimulateAutoPick(t.id)}
                                    >
                                        ⏱️ Simulate 4h Auto-Pick
                                    </button>
                                </div>
                            </div>
                        </div>
                    ))
                )}
            </div>

            {/* SECTION: MY ASSIGNED IN-PROGRESS TICKETS */}
            <div style={{ marginBottom: '20px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                    <h3 style={{ margin: 0, fontSize: '14px', letterSpacing: '-0.01em' }}>
                        My Active Assignments ({myAssignedTickets.length})
                    </h3>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                        48 hours to complete · Upload proof when done
                    </span>
                </div>

                {myAssignedTickets.length === 0 ? (
                    <div style={{ padding: '14px', background: '#fafafa', borderRadius: '8px', textAlign: 'center', fontSize: '12px', color: 'var(--text-muted)' }}>
                        You currently have no assigned refills.
                    </div>
                ) : (
                    myAssignedTickets.map(t => (
                        <div key={t.id} className="ticket-card" style={{ borderColor: 'var(--primary)' }}>
                            <div className="ticket-header">
                                <div>
                                    <div className="ticket-title">{t.snackName} ({t.quantity} units)</div>
                                    <div className="ticket-meta">{t.counterName} · {t.containerId}</div>
                                </div>
                                <div className="ticket-badges">
                                    <span className="tag tag-ack">Owner: You</span>
                                    {t.isAutoAssigned && <span className="tag tag-warning">Auto-Selected</span>}
                                </div>
                            </div>

                            <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                                ⏰ 48h Deadline: <strong>{t.completionDeadline ? new Date(t.completionDeadline).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'In 48 hours'}</strong>
                            </div>

                            {/* Actions on assigned ticket */}
                            <div style={{ display: 'flex', gap: '8px', marginTop: '6px', flexWrap: 'wrap' }}>
                                <button
                                    type="button"
                                    className="btn-sm btn-primary"
                                    onClick={() => {
                                        setActiveProofTicketId(t.id);
                                        setProofAmount(t.reservedBudget.toString());
                                    }}
                                >
                                    📸 Upload Proof & Refill Done
                                </button>
                                <button
                                    type="button"
                                    className="btn-sm btn-secondary"
                                    onClick={() => {
                                        setTransferTicketId(t.id);
                                        setTransferTargetId(transferCandidates[0]?.id ?? '');
                                    }}
                                >
                                    🔄 Transfer Ticket
                                </button>
                                <button
                                    type="button"
                                    className="btn-sm btn-danger"
                                    title="Simulate 48h deadline expiration to test automatic reassignment"
                                    onClick={() => void handleSimulateExpiry(t.id)}
                                >
                                    ⌛ Simulate 48h Expiry
                                </button>
                            </div>

                            {/* Inline Proof Form */}
                            {activeProofTicketId === t.id && (
                                <form
                                    onSubmit={e => void handleProofSubmit(e, t)}
                                    style={{ marginTop: '12px', padding: '12px', background: 'var(--surface-alt)', borderRadius: '8px', border: '1px solid var(--border)' }}
                                >
                                    <strong style={{ fontSize: '12px', display: 'block', marginBottom: '8px' }}>
                                        Step 8: Submit Proof of Purchase
                                    </strong>

                                    <label>
                                        Actual amount spent ({t.currency})
                                        <input
                                            type="number"
                                            step="0.01"
                                            min="0.01"
                                            placeholder={`Estimated: ${t.reservedBudget} ${t.currency}`}
                                            value={proofAmount}
                                            onChange={e => setProofAmount(e.target.value)}
                                            required
                                        />
                                    </label>

                                    {parseFloat(proofAmount) > t.reservedBudget && (
                                        <div style={{ fontSize: '11px', color: '#856404', background: '#fff3cd', padding: '6px 10px', borderRadius: '6px', marginBottom: '10px' }}>
                                            ⚠️ Actual spend ({proofAmount} {t.currency}) exceeds estimated reservation ({t.reservedBudget} {t.currency}). Per rules, it is flagged as <code>ProofExceedsEstimate</code> but never blocked.
                                        </div>
                                    )}

                                    <label>
                                        Items purchased
                                        <input
                                            value={proofItems}
                                            onChange={e => setProofItems(e.target.value)}
                                            required
                                        />
                                    </label>

                                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                                        <label>
                                            Receipt reference
                                            <input
                                                value={proofReceipt}
                                                onChange={e => setProofReceipt(e.target.value)}
                                                required
                                            />
                                        </label>
                                        <label>
                                            Counter photo reference
                                            <input
                                                value={proofPhoto}
                                                onChange={e => setProofPhoto(e.target.value)}
                                                required
                                            />
                                        </label>
                                    </div>

                                    <div style={{ display: 'flex', gap: '8px', marginTop: '10px' }}>
                                        <button type="submit" className="btn-sm btn-primary" disabled={isActionInProgress}>
                                            Submit Proof & File HR Claim →
                                        </button>
                                        <button
                                            type="button"
                                            className="btn-sm btn-secondary"
                                            onClick={() => setActiveProofTicketId(null)}
                                        >
                                            Cancel
                                        </button>
                                    </div>
                                </form>
                            )}
                        </div>
                    ))
                )}
            </div>

            {/* Other Volunteers' Active Tickets */}
            {otherAssignedTickets.length > 0 && (
                <div>
                    <h3 style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '8px' }}>
                        In-Progress by Other Volunteers ({otherAssignedTickets.length})
                    </h3>
                    {otherAssignedTickets.map(t => (
                        <div key={t.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 12px', background: '#fafafa', borderRadius: '6px', marginBottom: '6px', fontSize: '12px' }}>
                            <span><strong>{t.snackName}</strong> ({t.counterName})</span>
                            <span style={{ color: 'var(--text-muted)' }}>Assigned to: {t.volunteerName}</span>
                        </div>
                    ))}
                </div>
            )}

            {error && <p style={{ color: 'var(--danger)', fontSize: '12px', marginTop: '12px' }}>{error}</p>}

            {/* MODAL: DEBT PROTECTION PROMPT (Rule 5) */}
            {debtModalTicketId && (
                <div className="modal-overlay" onClick={() => setDebtModalTicketId(null)}>
                    <div className="modal-content" onClick={e => e.stopPropagation()}>
                        <h3>⚠️ Financial Protection Notice</h3>
                        <p style={{ fontSize: '13px', lineHeight: '1.5', color: '#14221c' }}>
                            "Your earlier claims are still being reimbursed. Another purchase will tie up more of your money. Continue?"
                        </p>
                        <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                            You currently have {currentVolunteer.unpaidClaimsCount} unpaid claims waiting for HR approval. If you choose not to proceed, nothing changes and the ticket stays open for another volunteer.
                        </p>
                        <div className="modal-actions">
                            <button
                                type="button"
                                className="btn-secondary"
                                onClick={() => setDebtModalTicketId(null)}
                            >
                                No, Keep Ticket Open
                            </button>
                            <button
                                type="button"
                                className="btn-primary"
                                onClick={() => void handleAcknowledge(debtModalTicketId, true)}
                            >
                                Yes, Continue & Accept
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* MODAL: TRANSFER TICKET (Rule 7) */}
            {transferTicketId && (
                <div className="modal-overlay" onClick={() => setTransferTicketId(null)}>
                    <div className="modal-content" onClick={e => e.stopPropagation()}>
                        <h3>Transfer Ticket to Another Volunteer</h3>
                        <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                            Transfer your assignment to another eligible volunteer in your office. The original 48-hour deadline stays the same.
                        </p>

                        <form onSubmit={handleTransferSubmit}>
                            <label>
                                Select Target Volunteer
                                <select
                                    value={transferTargetId}
                                    onChange={e => setTransferTargetId(e.target.value)}
                                    required
                                >
                                    {transferCandidates.length === 0 ? (
                                        <option value="">No other eligible volunteers available</option>
                                    ) : (
                                        transferCandidates.map(v => (
                                            <option key={v.id} value={v.id}>
                                                {v.name} ({v.openTicketsCount}/2 tickets · {v.unpaidClaimsCount} unpaid claims)
                                            </option>
                                        ))
                                    )}
                                </select>
                            </label>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn-secondary"
                                    onClick={() => setTransferTicketId(null)}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="btn-primary"
                                    disabled={!transferTargetId || isActionInProgress}
                                >
                                    Confirm Transfer →
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}

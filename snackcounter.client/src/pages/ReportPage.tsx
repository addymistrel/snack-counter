import { useState, useEffect } from 'react';
import type { FormEvent } from 'react';
import {
    getApiError,
    reportContainer,
    reportAlreadyRefilled,
    type Office,
    type ContainerCatalogItem,
    type Ticket,
    type Volunteer
} from '../api/client';

interface ReportPageProps {
    offices: Office[];
    containers: ContainerCatalogItem[];
    tickets: Ticket[];
    currentVolunteer: Volunteer;
    initialContainerId?: string;
    onCompleted: (message: string) => Promise<void>;
}

export function ReportPage({
    offices,
    containers,
    tickets,
    currentVolunteer,
    initialContainerId = '',
    onCompleted
}: ReportPageProps) {
    const [selectedOfficeId, setSelectedOfficeId] = useState(offices[0]?.id ?? 'oslo');
    const [selectedContainerId, setSelectedContainerId] = useState(initialContainerId || containers[0]?.containerId || '');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [mode, setMode] = useState<'qr' | 'manual'>('qr');

    // 3 Mandatory Checkboxes
    const [checkRightContainer, setCheckRightContainer] = useState(false);
    const [checkEmpty, setCheckEmpty] = useState(false);
    const [checkSure, setCheckSure] = useState(false);

    // Side-path Modal: Already refilled
    const [showAlreadyRefilledModal, setShowAlreadyRefilledModal] = useState(false);
    const [refillAmount, setRefillAmount] = useState('350');
    const [refillItems, setRefillItems] = useState('Assorted snacks');
    const [refillReceipt, setRefillReceipt] = useState('receipt-sidepath.jpg');

    // Filter containers by selected office
    const officeContainers = containers.filter(c => c.officeId.toLowerCase() === selectedOfficeId.toLowerCase());
    const currentContainer = containers.find(c => c.containerId === selectedContainerId) || officeContainers[0];

    useEffect(() => {
        if (initialContainerId) {
            // QR navigation can change while the page remains mounted.
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setSelectedContainerId(initialContainerId);
            const matching = containers.find(c => c.containerId === initialContainerId);
            if (matching) setSelectedOfficeId(matching.officeId);
        }
    }, [initialContainerId, containers]);

    // Check if container already has an active ticket
    const activeTicket = tickets.find(t =>
        t.containerId.toLowerCase() === currentContainer?.containerId?.toLowerCase() &&
        t.status !== 'Closed' &&
        t.status !== 'Cancelled'
    );

    const estimatedCost = currentContainer ? currentContainer.defaultQuantity * currentContainer.defaultUnitPrice : 0;
    const currentOffice = offices.find(o => o.id === selectedOfficeId) ?? offices[0];

    async function handleReport(event: FormEvent) {
        event.preventDefault();
        if (!currentContainer) return;

        if (!checkRightContainer || !checkEmpty || !checkSure) {
            setError('Please confirm all 3 checks: right container, verified empty, and ready to report.');
            return;
        }

        setIsSubmitting(true);
        setError('');

        try {
            await reportContainer({
                containerId: currentContainer.containerId,
                officeId: currentContainer.officeId,
                counterName: currentContainer.counterName,
                snackName: currentContainer.snackName,
                quantity: currentContainer.defaultQuantity,
                unitPrice: currentContainer.defaultUnitPrice,
                reporterId: currentVolunteer.id,
                reporterName: currentVolunteer.name,
                containerEmpty: true,
                locationConfirmed: true,
                snackTypeConfirmed: true,
            });

            setCheckRightContainer(false);
            setCheckEmpty(false);
            setCheckSure(false);

            await onCompleted(`Refill ticket created for ${currentContainer.snackName}. Eligible volunteers notified.`);
        } catch (err) {
            setError(getApiError(err, 'Unable to create ticket.'));
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleSidePathRefill(event: FormEvent) {
        event.preventDefault();
        if (!currentContainer) return;

        setIsSubmitting(true);
        setError('');

        try {
            const amountNum = parseFloat(refillAmount);
            await reportAlreadyRefilled({
                containerId: currentContainer.containerId,
                officeId: currentContainer.officeId,
                reporterId: currentVolunteer.id,
                reporterName: currentVolunteer.name,
                amount: amountNum,
                items: refillItems.split(',').map(s => s.trim()),
                receiptFileName: refillReceipt,
            });

            setShowAlreadyRefilledModal(false);
            await onCompleted(`Container recorded as already refilled. Active tickets closed; exception claim filed in ${currentVolunteer.name}'s name.`);
        } catch (err) {
            setError(getApiError(err, 'Unable to record already-refilled exception.'));
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <div className="card">
            <div className="card-title">
                <span className="step-badge">Step 1 & 2</span>
                <div>
                    <h2>Report an Empty Container</h2>
                    <p style={{ margin: 0, fontSize: '12px', color: 'var(--text-muted)' }}>
                        Scan QR code or choose container manually. Company SSO identity attached.
                    </p>
                </div>
            </div>

            {/* Mode Toggle */}
            <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
                <button
                    type="button"
                    className={`btn-sm ${mode === 'qr' ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setMode('qr')}
                >
                    📷 Scan QR Mode (/r/{selectedContainerId || 'id'})
                </button>
                <button
                    type="button"
                    className={`btn-sm ${mode === 'manual' ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setMode('manual')}
                >
                    📍 Manual Selection
                </button>
            </div>

            {/* Office Picker */}
            <label>
                Office Location
                <select
                    value={selectedOfficeId}
                    onChange={e => {
                        setSelectedOfficeId(e.target.value);
                        const first = containers.find(c => c.officeId.toLowerCase() === e.target.value.toLowerCase());
                        if (first) setSelectedContainerId(first.containerId);
                    }}
                >
                    {offices.map(o => (
                        <option key={o.id} value={o.id}>
                            {o.name} ({o.currency} · Budget: {o.monthlyBudget.toLocaleString()} {o.currency})
                        </option>
                    ))}
                </select>
            </label>

            {/* QR Mode: Visual grid of boxes */}
            {mode === 'qr' && (
                <div>
                    <span style={{ fontSize: '11px', fontWeight: 700, color: 'var(--text-muted)', display: 'block', marginBottom: '8px' }}>
                        Click a Snack Container to Simulate QR Scan:
                    </span>
                    <div className="qr-grid">
                        {officeContainers.map(c => {
                            const isSelected = c.containerId === currentContainer?.containerId;
                            const hasActive = tickets.some(t => t.containerId === c.containerId && t.status !== 'Closed' && t.status !== 'Cancelled');
                            return (
                                <div
                                    key={c.containerId}
                                    className={`qr-box ${isSelected ? 'selected' : ''}`}
                                    onClick={() => setSelectedContainerId(c.containerId)}
                                >
                                    <div className="qr-icon">{c.snackName.includes('Chocolate') ? '🍫' : c.snackName.includes('Almonds') || c.snackName.includes('Peanuts') ? '🥜' : c.snackName.includes('Water') ? '🥤' : '🍪'}</div>
                                    <span className="qr-title">{c.snackName}</span>
                                    <span className="qr-sub">{c.containerId}</span>
                                    {hasActive && (
                                        <span className="tag tag-ack" style={{ marginTop: '4px', display: 'inline-block', fontSize: '9px' }}>
                                            In Progress
                                        </span>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}

            {/* Manual Mode: Dropdown */}
            {mode === 'manual' && (
                <label>
                    Select Container
                    <select
                        value={currentContainer?.containerId ?? ''}
                        onChange={e => setSelectedContainerId(e.target.value)}
                    >
                        {officeContainers.map(c => (
                            <option key={c.containerId} value={c.containerId}>
                                {c.counterName} · {c.snackName} ({c.containerId})
                            </option>
                        ))}
                    </select>
                </label>
            )}

            {/* Container Details & Snapshot */}
            {currentContainer && (
                <div style={{ background: 'var(--surface-alt)', padding: '12px 14px', borderRadius: '10px', marginBottom: '16px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px', fontWeight: 700 }}>
                        <span>{currentContainer.snackName}</span>
                        <span>{estimatedCost.toLocaleString()} {currentOffice.currency}</span>
                    </div>
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px' }}>
                        {currentContainer.counterName} · Snapshot: {currentContainer.defaultQuantity} units @ {currentContainer.defaultUnitPrice} {currentOffice.currency}
                    </div>
                </div>
            )}

            {/* Duplicate Ticket Alert */}
            {activeTicket && (
                <div style={{ background: '#fff3cd', color: '#856404', padding: '12px', borderRadius: '8px', fontSize: '12px', marginBottom: '14px', border: '1px solid #ffeeba' }}>
                    <strong>Refill already in progress!</strong>
                    <div>Ticket #{activeTicket.id.slice(0, 8)} is already active ({activeTicket.status}) for this container. Quantity and price are locked to prevent duplicate shopping.</div>
                </div>
            )}

            {/* Form with 3 checks */}
            <form onSubmit={handleReport}>
                <div style={{ borderTop: '1px solid var(--border)', paddingTop: '12px', marginBottom: '16px' }}>
                    <span style={{ fontSize: '11px', fontWeight: 700, textTransform: 'uppercase', color: 'var(--text-muted)', display: 'block', marginBottom: '8px' }}>
                        Mandatory 3 Checks:
                    </span>
                    <label className="checkbox-row">
                        <input
                            type="checkbox"
                            checked={checkRightContainer}
                            onChange={e => setCheckRightContainer(e.target.checked)}
                        />
                        1. Confirmed right container ({currentContainer?.containerId})
                    </label>
                    <label className="checkbox-row">
                        <input
                            type="checkbox"
                            checked={checkEmpty}
                            onChange={e => setCheckEmpty(e.target.checked)}
                        />
                        2. Verified container is really empty
                    </label>
                    <label className="checkbox-row">
                        <input
                            type="checkbox"
                            checked={checkSure}
                            onChange={e => setCheckSure(e.target.checked)}
                        />
                        3. Sure / ready to request refill
                    </label>
                </div>

                <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                    <button
                        type="submit"
                        className="btn-primary"
                        disabled={isSubmitting || !!activeTicket || !checkRightContainer || !checkEmpty || !checkSure}
                    >
                        {isSubmitting ? 'Creating ticket…' : 'Create Refill Ticket →'}
                    </button>

                    <button
                        type="button"
                        className="btn-secondary"
                        onClick={() => setShowAlreadyRefilledModal(true)}
                    >
                        ✨ Already Refilled? (Side Path)
                    </button>
                </div>

                {error && <p style={{ color: 'var(--danger)', fontSize: '12px', marginTop: '10px' }}>{error}</p>}
            </form>

            {/* Modal: Side Path Already Refilled */}
            {showAlreadyRefilledModal && (
                <div className="modal-overlay" onClick={() => setShowAlreadyRefilledModal(false)}>
                    <div className="modal-content" onClick={e => e.stopPropagation()}>
                        <h3>Side Path: Already Refilled Without a Ticket</h3>
                        <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                            Did someone already buy snacks and restock this container? Record it now so nobody shops twice.
                            The active ticket will be closed and an exception claim will be filed directly in your name ({currentVolunteer.name}).
                        </p>

                        <form onSubmit={handleSidePathRefill}>
                            <label>
                                Amount spent ({currentOffice.currency})
                                <input
                                    type="number"
                                    step="0.01"
                                    min="1"
                                    value={refillAmount}
                                    onChange={e => setRefillAmount(e.target.value)}
                                    required
                                />
                            </label>

                            <label>
                                Items purchased
                                <input
                                    value={refillItems}
                                    onChange={e => setRefillItems(e.target.value)}
                                    required
                                />
                            </label>

                            <label>
                                Receipt file reference
                                <input
                                    value={refillReceipt}
                                    onChange={e => setRefillReceipt(e.target.value)}
                                    required
                                />
                            </label>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn-secondary"
                                    onClick={() => setShowAlreadyRefilledModal(false)}
                                >
                                    Cancel
                                </button>
                                <button type="submit" className="btn-primary" disabled={isSubmitting}>
                                    Submit Exception Claim →
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}

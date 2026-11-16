interface WalkthroughModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSelectStep: (tab: 'hub' | 'chat' | 'admin' | 'report') => void;
}

export function WalkthroughModal({ isOpen, onClose, onSelectStep }: WalkthroughModalProps) {
    if (!isOpen) return null;

    return (
        <div className="modal-overlay" onClick={onClose}>
            <div className="modal-content" style={{ maxWidth: '780px', maxHeight: '88vh', overflowY: 'auto' }} onClick={e => e.stopPropagation()}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--border)', paddingBottom: '12px', marginBottom: '16px' }}>
                    <div>
                        <h3 style={{ margin: 0 }}>10-Minute Walkthrough & Demonstration Guide</h3>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                            Follow this script to demonstrate all 11 workflow phases + side path to reviewers.
                        </span>
                    </div>
                    <button type="button" className="btn-sm btn-secondary" onClick={onClose}>✕ Close</button>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '14px', fontSize: '12px', lineHeight: '1.6' }}>
                    <div style={{ background: 'var(--surface-alt)', padding: '10px 14px', borderRadius: '8px' }}>
                        <strong>Presenter Tip:</strong> Use the top SSO Persona switcher to swap between <em>Astrid Lind (2 unpaid claims)</em>, <em>Lars Holm (0 unpaid claims)</em>, <em>Kari (Employee)</em>, and <em>Admin</em>.
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: 'var(--primary)' }}>
                        <strong>Phase 1: Report & Active Ticket Lock (Steps 1 & 2)</strong>
                        <div>• In "Workflow & Refill Hub", click any container box (e.g. <code>pantry-oslo-01</code>) to simulate scanning its QR code.</div>
                        <div>• Check all 3 mandatory boxes (Right container, Really empty, Sure). Click <em>Create Refill Ticket</em>.</div>
                        <div>• Click it again to prove duplicate prevention: system displays <em>"Refill already in progress"</em> with locked quantity & price snapshot!</div>
                        <button className="btn-sm btn-secondary" style={{ marginTop: '6px' }} onClick={() => { onSelectStep('hub'); onClose(); }}>
                            Go to Refill Hub →
                        </button>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#0d6efd' }}>
                        <strong>Phase 2: Budget Reservation & Admin Gate (Step 3)</strong>
                        <div>• The estimated cost is reserved from the monthly budget (6,000 NOK in Oslo, 15,000 INR in Bengaluru).</div>
                        <div>• If a report exceeds the remaining budget, it halts at <em>AwaitingBudgetApproval</em>. Admin can approve or decline in the <em>HR & Admin Control Room</em>.</div>
                        <button className="btn-sm btn-secondary" style={{ marginTop: '6px' }} onClick={() => { onSelectStep('admin'); onClose(); }}>
                            View Admin Control Room →
                        </button>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#198754' }}>
                        <strong>Phase 3: Simulated Chat & Email Notifications (Step 4)</strong>
                        <div>• Switch to the <em>Simulated Chat & Emails</em> tab.</div>
                        <div>• Notice instant Slack/Teams chat alerts and emails dispatched to all eligible volunteers with one-time action links. Ineligible volunteers (on leave or &gt;= 2 tickets) are automatically excluded.</div>
                        <button className="btn-sm btn-secondary" style={{ marginTop: '6px' }} onClick={() => { onSelectStep('chat'); onClose(); }}>
                            View Live Notification Feed →
                        </button>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#fd7e14' }}>
                        <strong>Phase 4: Volunteer Accept & Debt Protection (Step 5)</strong>
                        <div>• Switch SSO user to <em>Astrid Lind</em> (who has 2 unpaid claims pending).</div>
                        <div>• Click <em>Accept Refill</em>: The system triggers the Financial Protection Prompt: <em>"Your earlier claims are still being reimbursed. Another purchase will tie up more of your money. Continue?"</em></div>
                        <div>• Decline to show the ticket stays open; or switch to <em>Lars Holm</em> (0 claims) to accept without warning!</div>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#6f42c1' }}>
                        <strong>Phase 5: Auto-Pick Simulation (Step 6)</strong>
                        <div>• Click <em>"Simulate 4h Auto-Pick"</em> on any open ticket.</div>
                        <div>• The system automatically runs the fair rotation algorithm: selects the volunteer with the fewest open tickets and longest time since their last ticket!</div>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#d63384' }}>
                        <strong>Phase 6: 48h Deadline, Transfers & Expiry (Step 7)</strong>
                        <div>• The assigned owner has 48 hours to complete the refill.</div>
                        <div>• Test <em>Transfer Ticket</em>: Lars transfers to another volunteer; deadline stays the same.</div>
                        <div>• Test <em>Simulate 48h Expiry</em>: Assignment is recorded as expired and the ticket is immediately handed off to the next candidate with a fresh 48h window!</div>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#20c997' }}>
                        <strong>Phase 7: Proof Submission & Excess Flagging (Step 8)</strong>
                        <div>• Click <em>Upload Proof & Refill Done</em>. Enter actual spend greater than estimate (e.g. 520 NOK vs 450 NOK).</div>
                        <div>• The system flags <code>ProofExceedsEstimate</code> but never blocks completion. Reservation converts to real spend.</div>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#0dcaf0' }}>
                        <strong>Phase 8: HR System Stand-In, Downtime & Idempotent Retries (Step 9)</strong>
                        <div>• In <em>HR & Admin Control Room</em>, click <em>Simulate HR Downtime</em>.</div>
                        <div>• Submitting proof queues the claim as <code>PendingRetry</code>.</div>
                        <div>• Bring HR back online and click <em>Retry Pending Claims</em>: it re-submits with the EXACT same reference ID (<code>SNACK-{'{id}'}</code>), ensuring zero duplicates.</div>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#198754' }}>
                        <strong>Phase 9: Nightly Batch Reimbursement Sync (Step 10)</strong>
                        <div>• Click <em>Run Nightly Batch Sync</em>.</div>
                        <div>• HR approves and marks claims as <code>Paid</code>. The ticket closes automatically and volunteer debt count decrements.</div>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#0d6efd' }}>
                        <strong>Phase 10: Management Control Room & Multi-Currency Report (Step 11)</strong>
                        <div>• Switch to <em>Monthly Management Report</em>.</div>
                        <div>• See Oslo in NOK, Bengaluru in INR, consolidated total in NOK at the locked monthly rate (0.13), and top snacks by quantity and spend.</div>
                    </div>

                    <div className="feed-item" style={{ borderLeftColor: '#ffc107' }}>
                        <strong>Side Path: "Already Refilled" Without a Ticket</strong>
                        <div>• An employee walking by taps <em>Already Refilled?</em>.</div>
                        <div>• The in-flight ticket closes immediately so nobody shops redundantly. An exception claim is submitted to Admin in the buyer's name and is NEVER credited to the assigned volunteer.</div>
                    </div>
                </div>

                <div className="modal-actions" style={{ marginTop: '16px' }}>
                    <button type="button" className="btn-primary" onClick={onClose}>
                        Ready to Start Walkthrough →
                    </button>
                </div>
            </div>
        </div>
    );
}

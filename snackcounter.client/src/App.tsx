import { useCallback, useEffect, useState } from 'react';
import {
    getApiError,
    getDashboard,
    getMonthlyReport,
    resetDemo,
    type Claim,
    type Dashboard,
    type MonthlyReport,
    type Volunteer
} from './api/client';
import { ReportPage } from './pages/ReportPage';
import { VolunteerPage } from './pages/VolunteerPage';
import { NotificationsHub } from './pages/NotificationsHub';
import { AdminControlRoom } from './pages/AdminControlRoom';
import { MonthlyReportPage } from './pages/MonthlyReportPage';
import { WalkthroughModal } from './pages/WalkthroughModal';
import './App.css';

function App() {
    const [dashboard, setDashboard] = useState<Dashboard>();
    const [monthlyReport, setMonthlyReport] = useState<MonthlyReport>();
    const [currentVolunteerId, setCurrentVolunteerId] = useState<string>('');
    const [activeTab, setActiveTab] = useState<'hub' | 'chat' | 'admin' | 'report'>('hub');
    const [toastMessage, setToastMessage] = useState('');
    const [error, setError] = useState('');
    const [isWalkthroughOpen, setIsWalkthroughOpen] = useState(false);

    const qrContainerId = getQrContainerId();

    const refresh = useCallback(async () => {
        setError('');
        try {
            const [nextDashboard, nextReport] = await Promise.all([
                getDashboard(),
                getMonthlyReport()
            ]);
            setDashboard(nextDashboard);
            setMonthlyReport(nextReport);

            if (!nextDashboard.volunteers.some(v => v.id === currentVolunteerId) && nextDashboard.volunteers.length > 0) {
                setCurrentVolunteerId(nextDashboard.volunteers[0].id);
            }
        } catch (requestError) {
            setError(getApiError(requestError, 'Unable to load data from the server.'));
        }
    }, [currentVolunteerId]);

    useEffect(() => {
        // Initial server synchronization belongs in the mount effect.
        // eslint-disable-next-line react-hooks/set-state-in-effect
        void refresh();
    }, [refresh]);

    async function handleCompleted(message: string, claim?: Claim) {
        const fullNotice = claim
            ? `${message} Claim ${claim.referenceId} is ${claim.status}.`
            : message;
        setToastMessage(fullNotice);
        await refresh();

        // Auto-hide toast after 5s
        setTimeout(() => setToastMessage(''), 5000);
    }

    async function handleResetDemo() {
        try {
            await resetDemo();
            setToastMessage('Demo state has been reset to clean initial seed data.');
            await refresh();
            setTimeout(() => setToastMessage(''), 4000);
        } catch (err) {
            setError(getApiError(err, 'Failed to reset demo.'));
        }
    }

    if (!dashboard) {
        return (
            <main style={{ padding: '60px 20px', textAlign: 'center' }}>
                <p style={{ fontSize: '15px', color: 'var(--text-muted)' }}>
                    {error || 'Connecting to Snack Counter Automation service…'}
                </p>
            </main>
        );
    }

    // Active SSO persona
    const currentVolunteer: Volunteer = dashboard.volunteers.find(v => v.id === currentVolunteerId)
        ?? dashboard.volunteers[0];

    return (
        <main>
            {/* TOP SSO & DEMO CONTROL BAR */}
            <header className="top-nav">
                <div className="sso-badge">
                    <div className="sso-avatar">
                        {currentVolunteer.name.slice(0, 2).toUpperCase()}
                    </div>
                    <div className="sso-details">
                        <span className="sso-name">{currentVolunteer.name} (SSO)</span>
                        <span className="sso-sub">
                            {currentVolunteer.currentOfficeId.toUpperCase()} Office · {currentVolunteer.payrollCurrency} Payroll · {currentVolunteer.unpaidClaimsCount} Unpaid Claims
                        </span>
                    </div>
                    <div className="sso-selector">
                        <select
                            value={currentVolunteer.id}
                            onChange={e => setCurrentVolunteerId(e.target.value)}
                            title="Switch Company SSO Persona"
                        >
                            {dashboard.volunteers.map(volunteer => (
                                <option key={volunteer.id} value={volunteer.id}>
                                    {volunteer.name} ({volunteer.currentOfficeId.toUpperCase()} · {volunteer.unpaidClaimsCount} unpaid claims)
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                <div className="demo-controls">
                    <button
                        type="button"
                        className="btn-sm btn-secondary"
                        onClick={() => setIsWalkthroughOpen(true)}
                        style={{ background: '#fff2eb', color: 'var(--primary)', borderColor: 'var(--primary)' }}
                    >
                        📋 10-Min Walkthrough Script
                    </button>

                    <button
                        type="button"
                        className="btn-sm btn-secondary"
                        onClick={() => void handleResetDemo()}
                    >
                        ↺ Reset Demo
                    </button>

                    <span className={`badge ${dashboard.isHRDown ? 'badge-offline' : 'badge-online'}`}>
                        {dashboard.isHRDown ? 'HR: OFFLINE' : 'HR: ONLINE'}
                    </span>
                </div>
            </header>

            {/* HERO TITLE & STATS */}
            <div className="hero">
                <div>
                    <span style={{ fontSize: '11px', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '0.14em', color: 'var(--primary)' }}>
                        Snack Counter Automation · Oslo & Bengaluru
                    </span>
                    <h1>
                        From empty box<br />
                        <span>to reimbursed volunteer.</span>
                    </h1>
                    <p style={{ margin: 0, fontSize: '14px', color: 'var(--text-muted)', maxWidth: '540px' }}>
                        Automating goodwill into fair rotations, immutable budget reservations, and automated payroll claims across Oslo and Bengaluru.
                    </p>
                </div>

                <div className="hero-stats">
                    <div className="stat-item">
                        <div className="stat-val">{dashboard.tickets.length}</div>
                        <div className="stat-label">Total Tickets</div>
                    </div>
                    <div className="stat-item">
                        <div className="stat-val">{dashboard.tickets.filter(t => t.status === 'Open').length}</div>
                        <div className="stat-label">Open for Claim</div>
                    </div>
                    <div className="stat-item">
                        <div className="stat-val">{dashboard.claims.length}</div>
                        <div className="stat-label">HR Claims</div>
                    </div>
                </div>
            </div>

            {/* 11-STEP INTERACTIVE WORKFLOW STEPPER */}
            <div className="stepper-container">
                <div className="stepper-header">Complete 11-Step Workflow & Side Path</div>
                <div className="stepper">
                    <div className={`step-pill ${activeTab === 'hub' ? 'active' : ''}`} onClick={() => setActiveTab('hub')}>
                        1. QR/Manual Report
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'hub' ? 'active' : ''}`} onClick={() => setActiveTab('hub')}>
                        2. Ticket Created
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'admin' ? 'active' : ''}`} onClick={() => setActiveTab('admin')}>
                        3. Budget Gate
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'chat' ? 'active' : ''}`} onClick={() => setActiveTab('chat')}>
                        4. Chat & Email Notify
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'hub' ? 'active' : ''}`} onClick={() => setActiveTab('hub')}>
                        5. Volunteer Accept & Debt
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'hub' ? 'active' : ''}`} onClick={() => setActiveTab('hub')}>
                        6. 4h Auto-Pick
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'hub' ? 'active' : ''}`} onClick={() => setActiveTab('hub')}>
                        7. 48h Refill & Transfers
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'hub' ? 'active' : ''}`} onClick={() => setActiveTab('hub')}>
                        8. Proof Upload
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'admin' ? 'active' : ''}`} onClick={() => setActiveTab('admin')}>
                        9. HR Claim
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'admin' ? 'active' : ''}`} onClick={() => setActiveTab('admin')}>
                        10. Nightly Batch Sync
                    </div>
                    <span className="step-arrow">→</span>
                    <div className={`step-pill ${activeTab === 'report' ? 'active' : ''}`} onClick={() => setActiveTab('report')}>
                        11. Management Report
                    </div>
                    <span className="step-arrow">|</span>
                    <div className={`step-pill ${activeTab === 'hub' ? 'active' : ''}`} onClick={() => setActiveTab('hub')} style={{ borderColor: '#e65100' }}>
                        ★ Side Path: Already Refilled
                    </div>
                </div>
            </div>

            {/* TAB NAVIGATION */}
            <nav className="nav-tabs">
                <button
                    type="button"
                    className={`nav-tab ${activeTab === 'hub' ? 'active' : ''}`}
                    onClick={() => setActiveTab('hub')}
                >
                    🏢 Workflow & Refill Hub
                </button>
                <button
                    type="button"
                    className={`nav-tab ${activeTab === 'chat' ? 'active' : ''}`}
                    onClick={() => setActiveTab('chat')}
                >
                    💬 Simulated Chat & Emails ({dashboard.notifications.length})
                </button>
                <button
                    type="button"
                    className={`nav-tab ${activeTab === 'admin' ? 'active' : ''}`}
                    onClick={() => setActiveTab('admin')}
                >
                    ⚙️ HR & Admin Control Room ({dashboard.claims.length})
                </button>
                <button
                    type="button"
                    className={`nav-tab ${activeTab === 'report' ? 'active' : ''}`}
                    onClick={() => setActiveTab('report')}
                >
                    📊 Monthly Management Report
                </button>
            </nav>

            {error && <p style={{ color: 'var(--danger)', fontSize: '13px', padding: '10px 14px', background: '#fde8e8', borderRadius: '8px' }}>{error}</p>}

            {/* TAB CONTENT */}
            {activeTab === 'hub' && (
                <div className="grid-2">
                    <ReportPage
                        offices={dashboard.offices}
                        containers={dashboard.containers}
                        tickets={dashboard.tickets}
                        currentVolunteer={currentVolunteer}
                        initialContainerId={qrContainerId}
                        onCompleted={handleCompleted}
                    />
                    <VolunteerPage
                        tickets={dashboard.tickets}
                        volunteers={dashboard.volunteers}
                        currentVolunteer={currentVolunteer}
                        onCompleted={handleCompleted}
                    />
                </div>
            )}

            {activeTab === 'chat' && (
                <NotificationsHub
                    notifications={dashboard.notifications}
                    onSelectAction={() => {
                        setActiveTab('hub');
                    }}
                />
            )}

            {activeTab === 'admin' && (
                <AdminControlRoom
                    tickets={dashboard.tickets}
                    claims={dashboard.claims}
                    offices={dashboard.offices}
                    isHRDown={dashboard.isHRDown}
                    onCompleted={handleCompleted}
                />
            )}

            {activeTab === 'report' && (
                <MonthlyReportPage
                    dashboard={dashboard}
                    report={monthlyReport}
                    onRefresh={refresh}
                />
            )}

            {/* FLOATING TOAST NOTIFICATION */}
            {toastMessage && (
                <div className="toast" role="status">
                    ✓ {toastMessage}
                </div>
            )}

            {/* WALKTHROUGH SCRIPT MODAL */}
            <WalkthroughModal
                isOpen={isWalkthroughOpen}
                onClose={() => setIsWalkthroughOpen(false)}
                onSelectStep={tab => setActiveTab(tab)}
            />
        </main>
    );
}

function getQrContainerId() {
    const match = window.location.pathname.match(/^\/r\/([^/]+)$/);
    return match ? decodeURIComponent(match[1]) : '';
}

export default App;

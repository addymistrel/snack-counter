import { useState } from 'react';
import type { NotificationMessage } from '../api/client';

interface NotificationsHubProps {
    notifications: NotificationMessage[];
    onSelectAction?: (url: string) => void;
}

export function NotificationsHub({ notifications, onSelectAction }: NotificationsHubProps) {
    const [filterChannel, setFilterChannel] = useState<'all' | 'chat' | 'email'>('all');

    const filtered = notifications.filter(n => {
        if (filterChannel === 'chat') return n.channel.toLowerCase().includes('chat');
        if (filterChannel === 'email') return n.channel.toLowerCase().includes('email');
        return true;
    });

    return (
        <div className="card">
            <div className="card-title">
                <span className="step-badge">Step 4</span>
                <div>
                    <h2>Simulated Chat & Email Hub</h2>
                    <p style={{ margin: 0, fontSize: '12px', color: 'var(--text-muted)' }}>
                        Live feed of internal chat messages (Slack/Teams) and emails dispatched to eligible volunteers.
                    </p>
                </div>
            </div>

            {/* Filter buttons */}
            <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
                <button
                    type="button"
                    className={`btn-sm ${filterChannel === 'all' ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setFilterChannel('all')}
                >
                    All Notifications ({notifications.length})
                </button>
                <button
                    type="button"
                    className={`btn-sm ${filterChannel === 'chat' ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setFilterChannel('chat')}
                >
                    💬 Chat Messages
                </button>
                <button
                    type="button"
                    className={`btn-sm ${filterChannel === 'email' ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setFilterChannel('email')}
                >
                    ✉️ Emails
                </button>
            </div>

            {/* Notification feed */}
            <div style={{ maxHeight: '480px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                {filtered.length === 0 ? (
                    <div style={{ padding: '24px', textAlign: 'center', color: 'var(--text-muted)', fontSize: '13px' }}>
                        No notifications sent yet. Create a refill ticket or trigger an event to generate messages!
                    </div>
                ) : (
                    filtered.map(n => {
                        const isChat = n.channel.toLowerCase().includes('chat');
                        return (
                            <div key={n.id} className="feed-item" style={{ borderLeftColor: isChat ? '#198754' : '#0d6efd' }}>
                                <div className="feed-header">
                                    <span style={{ fontWeight: 700, color: isChat ? '#198754' : '#0d6efd' }}>
                                        {isChat ? '💬 Internal Chat' : '✉️ Email Notification'} · To: {n.recipientName}
                                    </span>
                                    <span>{new Date(n.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</span>
                                </div>
                                <div className="feed-title">{n.title}</div>
                                <div className="feed-body">{n.body}</div>
                                {n.actionUrl && (
                                    <div style={{ marginTop: '6px' }}>
                                        <button
                                            type="button"
                                            className="btn-sm btn-secondary"
                                            style={{ fontSize: '10px', padding: '3px 8px' }}
                                            onClick={() => onSelectAction?.(n.actionUrl)}
                                        >
                                            🔗 One-Time Action Link: {n.actionUrl}
                                        </button>
                                    </div>
                                )}
                            </div>
                        );
                    })
                )}
            </div>
        </div>
    );
}

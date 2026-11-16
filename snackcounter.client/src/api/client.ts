import axios from 'axios';
import type { AxiosError } from 'axios';

export interface Office {
    id: string;
    name: string;
    currency: string;
    monthlyBudget: number;
}

export interface ContainerCatalogItem {
    containerId: string;
    officeId: string;
    counterName: string;
    snackName: string;
    defaultQuantity: number;
    defaultUnitPrice: number;
}

export interface Volunteer {
    id: string;
    name: string;
    email: string;
    homeOfficeId: string;
    currentOfficeId: string;
    payrollCurrency: string;
    isActive: boolean;
    isOnLeave: boolean;
    openTicketsCount: number;
    unpaidClaimsCount: number;
    lastAssignedAt?: string;
}

export type TicketStatus =
    | 'Open'
    | 'AwaitingBudgetApproval'
    | 'Acknowledged'
    | 'Submitted'
    | 'Closed'
    | 'Cancelled';

export interface Ticket {
    id: string;
    containerId: string;
    counterName: string;
    snackName: string;
    quantity: number;
    unitPrice: number;
    officeId: string;
    officeName: string;
    currency: string;
    reservedBudget: number;
    status: TicketStatus;
    volunteerId?: string;
    volunteerName?: string;
    createdAt: string;
    acknowledgementDeadline: string;
    completionDeadline?: string;
    spentAmount?: number;
    items: string[];
    receiptFileName?: string;
    counterPhotoFileName?: string;
    proofExceedsEstimate: boolean;
    isAutoAssigned: boolean;
    expiredVolunteerId?: string;
    closedReason?: string;
    reporterId?: string;
    reporterName?: string;
}

export interface NotificationMessage {
    id: string;
    officeId: string;
    channel: string; // 'Chat' or 'Email'
    recipientId: string;
    recipientName: string;
    title: string;
    body: string;
    actionUrl: string;
    timestamp: string;
}

export type ClaimStatus =
    | 'PendingRetry'
    | 'Submitted'
    | 'Approved'
    | 'Paid'
    | 'Rejected';

export interface Claim {
    id: string;
    ticketId: string;
    referenceId: string;
    volunteerId: string;
    volunteerName: string;
    amount: number;
    currency: string;
    status: ClaimStatus;
    createdAt: string;
    settledAt?: string;
    rejectionReason?: string;
    isException: boolean;
    exceptionReporterName?: string;
}

export interface Dashboard {
    offices: Office[];
    containers: ContainerCatalogItem[];
    volunteers: Volunteer[];
    tickets: Ticket[];
    notifications: NotificationMessage[];
    claims: Claim[];
    isHRDown: boolean;
}

export interface OfficeSummary {
    officeId: string;
    officeName: string;
    currency: string;
    budget: number;
    reserved: number;
    spent: number;
    openTickets: number;
    closedTickets: number;
}

export interface SnackSummary {
    name: string;
    quantity: number;
    spend: number;
    currency: string;
}

export interface MonthlyReport {
    month: string;
    consolidatedNokTotal: number;
    lockedExchangeRateInrToNok: number;
    offices: OfficeSummary[];
    topSnacks: SnackSummary[];
}

interface ApiErrorResponse {
    error?: string;
    requiresConsent?: boolean;
    message?: string;
}

const api = axios.create({
    baseURL: '/api/v1/snack-counter',
    headers: { 'Content-Type': 'application/json' },
});

export function getApiError(error: unknown, fallback: string): string {
    if (axios.isAxiosError<ApiErrorResponse>(error)) {
        return error.response?.data?.error ?? error.response?.data?.message ?? error.message ?? fallback;
    }
    return fallback;
}

export async function getDashboard(): Promise<Dashboard> {
    const response = await api.get<Dashboard>('/dashboard');
    return response.data;
}

export async function getMonthlyReport(month?: string): Promise<MonthlyReport> {
    const response = await api.get<MonthlyReport>('/reports/monthly', {
        params: month ? { month } : undefined
    });
    return response.data;
}

export async function reportContainer(input: {
    containerId: string;
    officeId: string;
    counterName: string;
    snackName: string;
    quantity: number;
    unitPrice: number;
    reporterId: string;
    reporterName: string;
    containerEmpty: boolean;
    locationConfirmed: boolean;
    snackTypeConfirmed: boolean;
}): Promise<Ticket> {
    const response = await api.post<Ticket>('/containers/report', input);
    return response.data;
}

export async function reportAlreadyRefilled(input: {
    containerId: string;
    officeId: string;
    reporterId: string;
    reporterName: string;
    amount: number;
    items: string[];
    receiptFileName: string;
}): Promise<{ ticket?: Ticket; claim?: Claim }> {
    const response = await api.post<{ ticket?: Ticket; claim?: Claim }>('/containers/already-refilled', input);
    return response.data;
}

export async function acknowledgeTicket(
    ticketId: string,
    volunteerId: string,
    reimbursementConsent = false
): Promise<Ticket> {
    const response = await api.post<Ticket>(`/tickets/${ticketId}/acknowledge`, {
        volunteerId,
        reimbursementConsent,
    });
    return response.data;
}

export async function transferTicket(
    ticketId: string,
    fromVolunteerId: string,
    toVolunteerId: string,
    reimbursementConsent = false
): Promise<Ticket> {
    const response = await api.post<Ticket>(`/tickets/${ticketId}/transfer`, {
        fromVolunteerId,
        toVolunteerId,
        reimbursementConsent,
    });
    return response.data;
}

export async function simulateAutoPick(ticketId: string): Promise<Ticket> {
    const response = await api.post<Ticket>(`/tickets/${ticketId}/simulate-auto-pick`);
    return response.data;
}

export async function simulateExpiry(ticketId: string): Promise<Ticket> {
    const response = await api.post<Ticket>(`/tickets/${ticketId}/simulate-expiry`);
    return response.data;
}

export async function approveBudget(
    ticketId: string,
    approved: boolean,
    adminNotes?: string
): Promise<Ticket> {
    const response = await api.post<Ticket>(`/tickets/${ticketId}/budget-approval`, {
        approved,
        adminNotes,
    });
    return response.data;
}

export async function submitProof(ticketId: string, input: {
    volunteerId: string;
    amount: number;
    items: string[];
    receiptFileName: string;
    counterPhotoFileName: string;
}): Promise<Ticket> {
    const response = await api.post<Ticket>(`/tickets/${ticketId}/proof`, input);
    return response.data;
}

export async function toggleHRDown(): Promise<{ isDown: boolean }> {
    const response = await api.post<{ isDown: boolean }>('/hr/toggle-down');
    return response.data;
}

export async function retryPendingClaims(): Promise<{ retriedCount: number }> {
    const response = await api.post<{ retriedCount: number }>('/hr/retry');
    return response.data;
}

export async function runNightlySync(): Promise<{ processedCount: number }> {
    const response = await api.post<{ processedCount: number }>('/hr/nightly-sync');
    return response.data;
}

export async function resetDemo(): Promise<void> {
    await api.post('/reset-demo');
}

export async function getClaim(ticketId: string): Promise<Claim> {
    const response = await api.get<Claim>(`/tickets/${ticketId}/claim`);
    return response.data;
}

export function isAxiosError(error: unknown): error is AxiosError<ApiErrorResponse> {
    return axios.isAxiosError<ApiErrorResponse>(error);
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

interface TripOption {
  tripId: string; number: string; branchId: string; completedAt: string; deliveredQuantity: number;
  productNameSnapshot: string; customerNameSnapshot: string; siteNameSnapshot: string;
}
interface QualityTest {
  testId: string; testAgeDays: number; dueAt: string; minimumStrengthMpaSnapshot: number | null;
  acceptanceReferenceSnapshot: string; status: string; strengthMpa: number | null; outcome: string; version: number;
}
interface QualitySample {
  sampleId: string; sampleNumber: string; tripId: string; salesOrderId: string; siteId: string | null;
  materialType: string; gradeSnapshot: string; castAt: string; cubeCount: number; status: string;
  version: number; tests: QualityTest[];
}
interface Dispute {
  disputeId: string; type: string; description: string; status: string; resolution: string;
  version: number; tripId: string | null; salesOrderId: string | null; sampleId: string | null; evidenceDocumentIds?: string[];
}
interface QualityReport {
  sampleCount: number; dueOrOverdueTests: number; reviewRequired: number; passCount: number;
  failCount: number; passPercent: number | null; openDisputes: number; closedDisputes: number;
}
interface TestDraft { strengthMpa: number | null; reportDocumentId: string; reason: string; }
interface QualityEvent { eventId: string; actorUserId: string; eventType: string; detailJson: string; occurredAt: string; }

@Component({
  selector: 'app-m19-quality',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="quality-page">
      <header class="page-header">
        <div><p class="eyebrow">M19 · Quality management</p><h1>Quality & disputes</h1>
          <p class="intro">Track material samples from completed deliveries, review test results against approved criteria, and resolve linked disputes.</p></div>
        <button class="secondary" type="button" (click)="reload()" [disabled]="loading">Refresh</button>
      </header>
      <p *ngIf="message" class="notice" role="status">{{ message }}</p>
      <p *ngIf="error" class="error" role="alert">{{ error }}</p>
      <section class="summary" *ngIf="report as r" aria-label="Quality summary">
        <article><span>Samples</span><strong>{{ r.sampleCount }}</strong></article>
        <article><span>Tests due / overdue</span><strong>{{ r.dueOrOverdueTests }}</strong></article>
        <article><span>Review required</span><strong>{{ r.reviewRequired }}</strong></article>
        <article><span>Failed tests</span><strong>{{ r.failCount }}</strong></article>
        <article><span>Open disputes</span><strong>{{ r.openDisputes }}</strong></article>
        <article><span>Pass rate</span><strong>{{ r.passPercent === null ? '—' : (r.passPercent + '%') }}</strong></article>
      </section>
      <nav class="tabs" aria-label="Quality workspaces">
        <button type="button" [class.active]="tab === 'samples'" (click)="selectTab('samples')">Samples & tests</button>
        <button type="button" [class.active]="tab === 'criteria'" (click)="selectTab('criteria')">Acceptance criteria</button>
        <button type="button" [class.active]="tab === 'disputes'" (click)="selectTab('disputes')">Disputes</button>
      </nav>
      <section *ngIf="tab === 'samples'" class="panel">
        <h2>Register a sample</h2>
        <p class="hint">Only completed, authorized trips appear. Acceptance limits are configured by your quality team; missing limits require review.</p>
        <form class="sample-form" (ngSubmit)="createSample()">
          <label>Completed delivery
            <select name="trip" [(ngModel)]="sampleDraft.tripId" required>
              <option value="">Choose a trip</option>
              <option *ngFor="let trip of trips" [value]="trip.tripId">{{ trip.number }} · {{ trip.customerNameSnapshot }} · {{ trip.productNameSnapshot }}</option>
            </select>
          </label>
          <label>Material type<select name="type" [(ngModel)]="sampleDraft.materialType"><option>Concrete</option><option>TMT</option><option>Cement</option><option>Other</option></select></label>
          <label>Grade<input name="grade" [(ngModel)]="sampleDraft.grade" maxlength="80" required placeholder="Use the delivered product grade"></label>
          <label>Cast date & time<input name="castAt" type="datetime-local" [(ngModel)]="sampleDraft.castAt" required></label>
          <label>Test specimens<select name="cubeCount" [(ngModel)]="sampleDraft.cubeCount"><option [ngValue]="3">3</option><option [ngValue]="6">6</option></select></label>
          <button class="primary" type="submit" [disabled]="loading || !sampleDraft.tripId">Register sample</button>
        </form>
        <p *ngIf="loading" class="hint">Loading quality records…</p>
        <div class="empty" *ngIf="!loading && samples.length === 0">No quality samples are recorded in your authorized scope.</div>
        <article class="sample-card" *ngFor="let sample of samples">
          <div class="sample-heading"><div><span class="eyebrow">{{ sample.sampleNumber }}</span><h3>{{ sample.materialType }} · {{ sample.gradeSnapshot }}</h3>
            <p>{{ sample.tripId }} · Cast {{ sample.castAt | date:'medium' }} · {{ sample.cubeCount }} specimens</p></div><span class="badge" [class.review]="sample.status === 'ReviewRequired'">{{ statusLabel(sample.status) }}</span></div>
          <div class="test-row" *ngFor="let test of sample.tests">
            <div class="test-info"><strong>{{ test.testAgeDays }}-day test</strong><span>Due {{ test.dueAt | date:'mediumDate' }}</span>
              <span *ngIf="test.minimumStrengthMpaSnapshot !== null">Limit {{ test.minimumStrengthMpaSnapshot }} MPa · {{ test.acceptanceReferenceSnapshot }}</span>
              <span *ngIf="test.minimumStrengthMpaSnapshot === null" class="warning">Acceptance criteria missing — review required</span>
              <span *ngIf="test.outcome !== 'Pending'">Result: {{ test.strengthMpa }} MPa · {{ statusLabel(test.outcome) }}</span></div>
            <div class="result-form" *ngIf="drafts[test.testId]">
              <label>MPa<input type="number" min="0" step="0.01" [(ngModel)]="drafts[test.testId].strengthMpa" [ngModelOptions]="{standalone:true}"></label>
              <label>Clean M18 report document ID (optional)<input [(ngModel)]="drafts[test.testId].reportDocumentId" [ngModelOptions]="{standalone:true}" placeholder="Document ID"></label>
              <label>Correction reason<input [(ngModel)]="drafts[test.testId].reason" [ngModelOptions]="{standalone:true}" placeholder="Required when correcting a result"></label>
              <button class="secondary" type="button" (click)="recordResult(sample, test)" [disabled]="loading || drafts[test.testId].strengthMpa === null">Record test result</button>
            </div>
          </div>
        </article>
      </section>
      <section *ngIf="tab === 'criteria'" class="panel">
        <h2>Acceptance criteria</h2>
        <p class="hint">Criteria are effective-dated, tenant-configured and immutable after creation. No universal strength values are assumed.</p>
        <form class="sample-form" (ngSubmit)="saveCriteria()">
          <label>Material<select name="criteriaType" [(ngModel)]="criteriaDraft.materialType"><option>Concrete</option><option>TMT</option><option>Cement</option><option>Other</option></select></label>
          <label>Grade<input name="criteriaGrade" [(ngModel)]="criteriaDraft.grade" required></label>
          <label>Test age (days)<input name="criteriaAge" type="number" min="1" max="365" [(ngModel)]="criteriaDraft.testAgeDays" required></label>
          <label>Minimum MPa<input name="criteriaStrength" type="number" min="0.01" step="0.01" [(ngModel)]="criteriaDraft.minimumStrengthMpa" required></label>
          <label>Standard / project reference<input name="reference" [(ngModel)]="criteriaDraft.standardReference" required></label>
          <label>Effective from<input name="effectiveFrom" type="date" [(ngModel)]="criteriaDraft.effectiveFrom" required></label>
          <label>Change reason<input name="changeReason" [(ngModel)]="criteriaDraft.changeReason" required></label>
          <button class="primary" type="submit" [disabled]="loading">Add criteria</button>
        </form>
        <div class="empty" *ngIf="criteria.length === 0">No criteria are configured yet. Samples without criteria remain explicitly review-required.</div>
        <div class="table-wrap"><table *ngIf="criteria.length"><thead><tr><th>Material / grade</th><th>Age</th><th>Limit</th><th>Standard</th><th>Effective</th></tr></thead><tbody>
          <tr *ngFor="let row of criteria"><td>{{ row.materialType }} · {{ row.grade }}</td><td>{{ row.testAgeDays }}d</td><td>{{ row.minimumStrengthMpa }} MPa</td><td>{{ row.standardReference }}</td><td>{{ row.effectiveFrom }}</td></tr>
        </tbody></table></div>
      </section>
      <section *ngIf="tab === 'disputes'" class="panel">
        <h2>Open a linked dispute</h2>
        <form class="sample-form" (ngSubmit)="createDispute()">
          <label>Related quality sample<select name="disputeSample" [(ngModel)]="disputeDraft.sampleId"><option value="">Choose a sample</option><option *ngFor="let sample of samples" [value]="sample.sampleId">{{ sample.sampleNumber }} · {{ sample.gradeSnapshot }}</option></select></label>
          <label>Dispute type<select name="disputeType" [(ngModel)]="disputeDraft.type"><option>Quality</option><option>Quantity</option><option>Delay</option><option>Billing</option><option>Other</option></select></label>
          <label class="wide">Description<textarea name="description" [(ngModel)]="disputeDraft.description" maxlength="2000" required></textarea></label>
          <button class="primary" type="submit" [disabled]="loading || !disputeDraft.sampleId">Open dispute</button>
        </form>
        <div class="empty" *ngIf="disputes.length === 0">No disputes are recorded in your authorized scope.</div>
        <article class="dispute-card" *ngFor="let dispute of disputes">
          <div><span class="badge" [class.review]="dispute.status === 'Open'">{{ dispute.status }}</span><h3>{{ dispute.type }}</h3><p>{{ dispute.description }}</p>
            <small>Order {{ dispute.salesOrderId || '—' }} · Trip {{ dispute.tripId || '—' }}</small></div>
          <div class="dispute-evidence">
            <button class="secondary" type="button" (click)="loadHistory(dispute)">View activity history</button>
            <label>Clean M18 document ID<input [(ngModel)]="evidenceDraft[dispute.disputeId]" [ngModelOptions]="{standalone:true}" placeholder="Document ID"></label>
            <button class="secondary" type="button" (click)="linkEvidence(dispute)" [disabled]="!evidenceDraft[dispute.disputeId]?.trim()">Link evidence</button>
            <div class="timeline" *ngFor="let event of histories[dispute.disputeId] || []"><strong>{{ event.eventType }}</strong><span>{{ event.occurredAt | date:'short' }} · {{ event.actorUserId }}</span></div>
          </div>
          <div class="dispute-actions" *ngIf="dispute.status === 'Open' || dispute.status === 'Investigating' || dispute.status === 'Escalated'">
            <label>Resolution / transition reason<input [(ngModel)]="disputeDraft.resolution" [ngModelOptions]="{standalone:true}" placeholder="Required to resolve"></label>
            <button class="secondary" type="button" (click)="changeDispute(dispute, 'Investigating')" *ngIf="dispute.status === 'Open'">Investigate</button>
            <button class="primary" type="button" (click)="changeDispute(dispute, 'Resolved')" [disabled]="!disputeDraft.resolution.trim()">Resolve</button>
          </div>
        </article>
      </section>
    </main>
  `,
  styles: [`
    :host{display:block;color:var(--color-text-primary,#28231f)}.quality-page{max-width:1440px;margin:0 auto;padding:24px}.page-header{display:flex;justify-content:space-between;align-items:flex-start;gap:20px;margin-bottom:24px}h1,h2,h3,p{margin-top:0}.page-header h1{margin-bottom:8px;font-size:30px}.intro,.hint{color:var(--color-text-secondary,#6d625a);max-width:850px}.eyebrow{display:block;color:#9a633f;text-transform:uppercase;font-size:11px;font-weight:700;letter-spacing:.08em;margin-bottom:6px}.summary{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px;margin:20px 0}.summary article,.panel,.sample-card,.dispute-card{background:#fff;border:1px solid #e5ded7;border-radius:10px}.summary article{padding:16px;display:grid;gap:8px}.summary span,.test-info span,small{color:#766c63;font-size:13px}.summary strong{font-size:22px;color:#523b2d}.tabs{display:flex;gap:8px;border-bottom:1px solid #ddd;margin-bottom:18px;overflow:auto}.tabs button{white-space:nowrap;border:0;background:none;padding:12px 16px;color:#665d56}.tabs button.active{color:#72472f;border-bottom:3px solid #a36b46;font-weight:700}.panel{padding:22px;margin-bottom:24px}.panel h2{margin-bottom:8px}.sample-form{display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:14px;padding:14px 0 22px}.sample-form label,.result-form label,.dispute-actions label{display:grid;gap:6px;font-size:13px;font-weight:600}.sample-form input,.sample-form select,.sample-form textarea,.result-form input,.dispute-actions input{width:100%;padding:10px;border:1px solid #cfc5bc;border-radius:6px;background:white;color:inherit;box-sizing:border-box}.sample-form textarea{min-height:78px}.wide{grid-column:1/-1}.primary,.secondary{border-radius:6px;padding:10px 16px;font-weight:700;cursor:pointer}.primary{background:#805437;color:white;border:1px solid #805437}.secondary{background:white;color:#68432e;border:1px solid #a98d78}.primary:disabled,.secondary:disabled{opacity:.55;cursor:not-allowed}.sample-card,.dispute-card{padding:18px;margin:14px 0}.sample-heading{display:flex;justify-content:space-between;gap:16px}.sample-heading h3,.dispute-card h3{margin-bottom:4px}.sample-heading p,.dispute-card p{color:#71675e;font-size:13px}.badge{display:inline-flex;align-self:flex-start;border-radius:20px;padding:5px 10px;background:#e4f2e9;color:#275a38;font-size:12px;font-weight:700}.badge.review{background:#fff1d8;color:#76501e}.test-row{display:grid;grid-template-columns:minmax(200px,1fr) minmax(280px,2fr);gap:16px;border-top:1px solid #eee7e1;padding:15px 0}.test-info{display:grid;gap:5px;align-content:start}.warning,.error{color:#9b382b}.result-form{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:10px;align-items:end}.notice{background:#edf6ef;border-left:4px solid #4a8058;padding:12px}.error{background:#fff0ed;padding:12px}.empty{padding:20px;background:#faf8f6;color:#70665e;border-radius:6px}.table-wrap{overflow-x:auto}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:10px;border-bottom:1px solid #eee}th{color:#6c5140}.dispute-card{display:flex;justify-content:space-between;gap:20px}.dispute-actions,.dispute-evidence{display:flex;gap:8px;align-items:end;flex-wrap:wrap}.dispute-evidence{margin-top:12px}.timeline{display:grid;gap:3px;border-left:2px solid #a36b46;padding:6px 10px}.timeline span{font-size:12px;color:#766c63}.dispute-actions label{min-width:210px}.dispute-actions input{min-width:220px}@media(max-width:760px){.quality-page{padding:16px}.page-header,.dispute-card{flex-direction:column}.test-row{grid-template-columns:1fr}.result-form{grid-template-columns:1fr}.sample-form{grid-template-columns:1fr}}
  `],
})
export class M19Page implements OnInit {
  private readonly api = inject(ApiClient);
  loading = false;
  error = '';
  message = '';
  tab: 'samples' | 'criteria' | 'disputes' = 'samples';
  trips: TripOption[] = [];
  samples: QualitySample[] = [];
  criteria: Array<{ criteriaId: string; materialType: string; grade: string; testAgeDays: number; minimumStrengthMpa: number; standardReference: string; effectiveFrom: string }> = [];
  disputes: Dispute[] = [];
  report: QualityReport | null = null;
  drafts: Record<string, TestDraft> = {};
  histories: Record<string, QualityEvent[]> = {};
  evidenceDraft: Record<string, string> = {};
  sampleDraft = { idempotencyKey: crypto.randomUUID(), tripId: '', materialType: 'Concrete', grade: '', castAt: this.localDateTime(), cubeCount: 3 };
  criteriaDraft = { materialType: 'Concrete', grade: '', testAgeDays: 7, minimumStrengthMpa: null as number | null, standardReference: '', effectiveFrom: new Date().toISOString().slice(0, 10), changeReason: '' };
  disputeDraft = { idempotencyKey: crypto.randomUUID(), sampleId: '', type: 'Quality', description: '', resolution: '' };

  ngOnInit(): void { this.reload(); }

  reload(): void {
    this.loading = true; this.error = '';
    this.api.post('m19/reminders/refresh', {}).subscribe({ error: () => undefined });
    this.api.get<TripOption[]>('m19/trip-options').subscribe({ next: (rows) => this.trips = rows, error: (e) => this.showError(e) });
    this.api.get<QualitySample[]>('m19/samples').subscribe({
      next: (rows) => { this.samples = rows; for (const sample of rows) for (const test of sample.tests) this.drafts[test.testId] ??= { strengthMpa: test.strengthMpa, reportDocumentId: '', reason: '' }; this.finishLoad(); },
      error: (e) => { this.showError(e); this.finishLoad(); },
    });
    this.api.get<QualityReport>('m19/reports').subscribe({ next: (row) => this.report = row, error: (e) => this.showError(e) });
  }

  selectTab(tab: 'samples' | 'criteria' | 'disputes'): void {
    this.tab = tab;
    if (tab === 'criteria') this.api.get<typeof this.criteria>('m19/criteria').subscribe({ next: (rows) => this.criteria = rows, error: (e) => this.showError(e) });
    if (tab === 'disputes') this.api.get<Dispute[]>('m19/disputes').subscribe({ next: (rows) => this.disputes = rows, error: (e) => this.showError(e) });
  }

  createSample(): void {
    const trip = this.trips.find((item) => item.tripId === this.sampleDraft.tripId);
    if (!trip || !this.sampleDraft.grade.trim()) return;
    this.loading = true; this.error = ''; this.message = '';
    this.api.post('m19/samples', { idempotencyKey: this.sampleDraft.idempotencyKey, tripId: trip.tripId, materialType: this.sampleDraft.materialType, grade: this.sampleDraft.grade.trim(), castAt: new Date(this.sampleDraft.castAt).toISOString(), cubeCount: Number(this.sampleDraft.cubeCount), ownerUserId: null })
      .subscribe({ next: () => { this.message = 'Sample registered by the server.'; this.sampleDraft.grade = ''; this.sampleDraft.idempotencyKey = crypto.randomUUID(); this.reload(); }, error: (e) => { this.showError(e); this.finishLoad(); } });
  }

  recordResult(sample: QualitySample, test: QualityTest): void {
    const draft = this.drafts[test.testId]; if (!draft || draft.strengthMpa === null) return;
    this.loading = true; this.error = ''; this.message = '';
    this.api.post(`m19/samples/${sample.sampleId}/tests/${test.testAgeDays}`, {
      strengthMpa: Number(draft.strengthMpa), testedAt: new Date().toISOString(), reportDocumentId: draft.reportDocumentId.trim() || null,
      reason: draft.reason.trim(), expectedVersion: test.version,
    }).subscribe({ next: () => { this.message = `${test.testAgeDays}-day result recorded.`; this.reload(); }, error: (e) => { this.showError(e); this.finishLoad(); } });
  }

  saveCriteria(): void {
    const d = this.criteriaDraft; if (!d.grade.trim() || d.minimumStrengthMpa === null) return;
    this.loading = true; this.error = ''; this.message = '';
    this.api.post('m19/criteria', { ...d, grade: d.grade.trim(), standardReference: d.standardReference.trim(), changeReason: d.changeReason.trim(), effectiveUntil: null })
      .subscribe({ next: () => { this.message = 'Acceptance criteria saved.'; this.selectTab('criteria'); this.finishLoad(); }, error: (e) => { this.showError(e); this.finishLoad(); } });
  }

  createDispute(): void {
    const sample = this.samples.find((row) => row.sampleId === this.disputeDraft.sampleId);
    if (!sample || !this.disputeDraft.description.trim()) return;
    this.loading = true; this.error = ''; this.message = '';
    this.api.post('m19/disputes', { idempotencyKey: this.disputeDraft.idempotencyKey, type: this.disputeDraft.type, description: this.disputeDraft.description.trim(), sampleId: sample.sampleId,
      salesOrderId: sample.salesOrderId, tripId: sample.tripId, customerPartyId: null, supplierPartyId: null, siteId: sample.siteId,
      challanId: null, invoiceId: null, ownerUserId: null })
      .subscribe({ next: () => { this.message = 'Dispute opened and linked to the selected sample.'; this.disputeDraft.description = ''; this.disputeDraft.idempotencyKey = crypto.randomUUID(); this.selectTab('disputes'); this.finishLoad(); }, error: (e) => { this.showError(e); this.finishLoad(); } });
  }

  loadHistory(dispute: Dispute): void {
    this.api.get<QualityEvent[]>(`m19/disputes/${dispute.disputeId}/history`).subscribe({
      next: (events) => this.histories[dispute.disputeId] = events,
      error: (e) => this.showError(e),
    });
  }

  linkEvidence(dispute: Dispute): void {
    const documentId = this.evidenceDraft[dispute.disputeId]?.trim();
    if (!documentId) return;
    this.api.post(`m19/disputes/${dispute.disputeId}/evidence/${documentId}`, {}).subscribe({
      next: () => { this.message = 'Clean M18 evidence linked.'; this.evidenceDraft[dispute.disputeId] = ''; this.selectTab('disputes'); },
      error: (e) => this.showError(e),
    });
  }
  changeDispute(dispute: Dispute, status: string): void {
    const resolution = this.disputeDraft.resolution.trim();
    this.loading = true; this.error = ''; this.message = '';
    this.api.post(`m19/disputes/${dispute.disputeId}/status`, { status, resolution: status === 'Resolved' ? resolution : '', reason: resolution || 'Assigned for investigation', creditNoteId: null, expectedVersion: dispute.version })
      .subscribe({ next: () => { this.message = `Dispute moved to ${status}.`; this.disputeDraft.resolution = ''; this.selectTab('disputes'); this.finishLoad(); }, error: (e) => { this.showError(e); this.finishLoad(); } });
  }

  statusLabel(status: string): string { return status === 'ReviewRequired' ? 'Review required' : status; }
  private finishLoad(): void { this.loading = false; }
  private showError(error: { error?: { detail?: string; title?: string }; message?: string }): void {
    this.error = error.error?.detail ?? error.error?.title ?? error.message ?? 'The request failed. Refresh and retry or contact an administrator.';
  }
  private localDateTime(): string { const date = new Date(); date.setMinutes(date.getMinutes() - date.getTimezoneOffset()); return date.toISOString().slice(0, 16); }
}
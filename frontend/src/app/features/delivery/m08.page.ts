import { CommonModule } from '@angular/common';
import { HttpEventType } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m08-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head">
        <div><p class="eyebrow">OPERATE · M08 DELIVERY</p><h1>Dispatch board</h1>
          <p class="muted">Approved delivery schedules, pour windows and trip progress.</p></div>
        <div class="head-actions"><input aria-label="Dispatch date" type="date" [(ngModel)]="date" (change)="load()">
          <button class="secondary" type="button" (click)="load()">Refresh</button></div>
      </header>
      <p class="notice" role="note">Receiver OTP is not used. Uploads stay private and quarantined until the configured malware scanner reports a clean result; only verified evidence can enter review.</p>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p>
      <p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <p class="loading" *ngIf="loading()">Loading dispatch board…</p>
      <section class="metrics" *ngIf="board() as data">
        <div><small>Scheduled</small><strong>{{ data.totals?.scheduled || 0 }}</strong></div>
        <div><small>Delivered</small><strong>{{ data.totals?.delivered || 0 }}</strong></div>
        <div><small>Pending</small><strong>{{ data.totals?.pending || 0 }}</strong></div>
        <div><small>Refresh interval</small><strong>30 sec</strong></div>
      </section>
      <section class="panel">
        <header><div><p class="eyebrow">RECEIVER REVIEW</p><h2>Pending delivery evidence</h2></div>
          <button class="secondary compact" type="button" (click)="loadPendingEvidence()">Refresh</button></header>
        <p class="error" role="alert" *ngIf="pendingError()">{{ pendingError() }}</p>
        <div class="empty" *ngIf="!pendingError() && !pendingEvidence().length">No submitted delivery evidence is pending review.</div>
        <article class="card" *ngFor="let item of pendingEvidence()">
          <div><strong>{{ item.number }} · {{ item.receiverName }}</strong>
            <small>Completed {{ item.completedAt | date:'medium' }} · Delivered {{ item.deliveredQuantity }} · {{ item.evidenceState }}</small>
            <small>Signature asset {{ item.signatureReference || '—' }} · Photo asset {{ item.photoReference || '—' }}</small></div>
          <button class="secondary compact" type="button" (click)="openEvidenceReview(item)">Review evidence</button>
        </article>
      </section>
      <section class="panel">
        <header><div><p class="eyebrow">UNSCHEDULED</p><h2>Approved M06 delivery rows</h2></div></header>
        <div class="empty" *ngIf="!board().unscheduled?.length">No planned M06 schedules are available for this date.</div>
        <article class="card" *ngFor="let row of board().unscheduled">
          <div><strong>{{ row.number }} · {{ row.customerNameSnapshot }}</strong>
            <small>Delivery {{ row.deliveryDate | date:'mediumDate' }} · {{ row.timeWindow || 'Time window not set' }} · {{ row.quantity }}</small>
            <small>Schedule {{ row.scheduleId }} · Product {{ row.productId }}</small></div>
          <button class="primary compact" type="button" (click)="openPour(row)">Schedule pour</button>
        </article>
      </section>
      <section class="panel">
        <header><div><p class="eyebrow">PLANT TIMELINE</p><h2>Pours and trips</h2></div></header>
        <div class="empty" *ngIf="!board().pours?.length">No pours are scheduled for this date.</div>
        <article class="pour" *ngFor="let entry of board().pours">
          <div class="pour-head">
            <div><strong>{{ entry.pour.customerNameSnapshot }} · {{ entry.pour.productNameSnapshot }}</strong>
              <small>{{ entry.pour.windowStart | date:'shortTime' }}–{{ entry.pour.windowEnd | date:'shortTime' }} · {{ entry.pour.siteNameSnapshot || 'Site not recorded' }}</small></div>
            <span class="chip">{{ entry.pour.status }}</span>
          </div>
          <p>Scheduled {{ entry.pour.scheduledQuantity }} · Delivered {{ entry.pour.deliveredQuantity }} · Remaining {{ entry.pour.scheduledQuantity - entry.pour.deliveredQuantity }}</p>
          <div class="card" *ngFor="let trip of entry.trips">
            <div><strong>{{ trip.number }} · {{ trip.status }}</strong>
              <small>Load {{ trip.loadedQuantity }} · Delivered {{ trip.deliveredQuantity }} · {{ trip.evidenceState }}</small></div>
            <div class="actions">
              <button *ngIf="trip.status === 'Planned'" class="secondary compact" type="button" (click)="act(trip, 'Loaded')">Loaded</button>
              <button *ngIf="trip.status === 'Loading'" class="secondary compact" type="button" (click)="act(trip, 'Dispatched')">Left plant</button>
              <button *ngIf="trip.status === 'Dispatched'" class="secondary compact" type="button" (click)="act(trip, 'At site')">At site</button>
              <button *ngIf="trip.status === 'At site'" class="secondary compact" type="button" (click)="act(trip, 'Unloading')">Unloading</button>
              <button *ngIf="trip.status === 'Unloading'" class="primary compact" type="button" (click)="act(trip, 'Completed')">Complete</button>
              <button *ngIf="trip.status === 'Completed' && (trip.evidenceState === 'Rejected' || trip.evidenceState === 'Missing')" class="secondary compact" type="button" (click)="openResubmission(trip)">Resubmit evidence</button>
            </div>
          </div>
          <button *ngIf="entry.pour.status === 'Scheduled' || entry.pour.status === 'In progress'"
            class="secondary compact" type="button" (click)="openPlanner(entry.pour)">Plan trips</button>
          <button class="secondary compact" type="button" (click)="openDetail(entry.pour)">Timeline</button>
        </article>
      </section>

      <div class="backdrop" *ngIf="evidenceReview()" (click)="closeEvidenceReview()"></div>
      <aside class="drawer" *ngIf="evidenceReview() as review" aria-label="Receiver evidence review">
        <header><div><p class="eyebrow">SCR-M08-11 · EVIDENCE REVIEW</p><h2>{{ review.number }}</h2></div>
          <button class="icon" type="button" (click)="closeEvidenceReview()" aria-label="Close">×</button></header>
        <p>Receiver {{ review.receiverName }} · completed {{ review.completedAt | date:'medium' }} · {{ review.deliveredQuantity }} delivered</p>
        <p class="notice" *ngIf="review.attachmentBlocker">{{ review.attachmentBlocker }} Verify is disabled until secure M18 evidence is available.</p>
        <div class="line" *ngFor="let asset of review.assets">
          <span>{{ asset.purpose }} · {{ asset.contentType }} · scan {{ asset.scanStatus }}</span>
          <div class="actions"><button class="secondary compact" type="button" (click)="previewEvidence(asset)">Preview securely</button><button class="secondary compact" type="button" (click)="downloadEvidence(asset)">Download</button></div>
        </div>
        <p class="notice" *ngIf="!review.assets?.length">No validated secure preview is available. Do not approve from an opaque reference alone.</p>
        <div class="line" *ngIf="previewUrl()">
          <img *ngIf="previewMime().startsWith('image/')" [src]="previewUrl()" alt="Securely loaded delivery evidence" style="max-width:100%;max-height:60vh">
          <iframe *ngIf="previewMime() === 'application/pdf'" [src]="safePreviewUrl()" title="Secure delivery evidence PDF" style="width:100%;height:60vh;border:0"></iframe>
        </div>
        <label>Decision reason<textarea [(ngModel)]="evidenceReason" name="evidenceReason" minlength="3" maxlength="1000" required></textarea></label>
        <div class="actions">
          <button class="primary compact" type="button" [disabled]="decisionSaving() || !review.canVerify || !evidenceReason.trim()" (click)="decideEvidence('Verify')">Verify</button>
          <button class="danger compact" type="button" [disabled]="decisionSaving() || !review.canReview || !evidenceReason.trim()" (click)="decideEvidence('Reject')">Reject</button>
        </div>
        <h3>Decision history</h3>
        <div class="line" *ngFor="let item of review.history"><span>{{ item.eventType }} · {{ item.actorUserId }}</span><small>{{ item.receivedAt | date:'medium' }} · {{ item.detailJson }}</small></div>
      </aside>

      <div class="backdrop" *ngIf="evidenceDraft()" (click)="evidenceDraft.set(null)"></div>
      <aside class="drawer" *ngIf="evidenceDraft() as draft" aria-label="Resubmit receiver evidence">
        <header><div><p class="eyebrow">M08 · RESUBMISSION</p><h2>{{ draft.trip.number }}</h2></div>
          <button class="icon" type="button" (click)="evidenceDraft.set(null)" aria-label="Close">×</button></header>
        <p class="notice">Select real receiver signature/photo files. Each file stays quarantined until the scanner confirms it is clean. Upload a corrected version after a rejected scan.</p>
        <form class="form-section" (ngSubmit)="submitEvidenceResubmission()">
          <label>Receiver name<input [(ngModel)]="draft.receiverName" name="resubmitReceiver" maxlength="160" required></label>
          <label>Receiver mobile<input [(ngModel)]="draft.receiverMobile" name="resubmitMobile" maxlength="24"></label>
          <label>Receiver signature file<input type="file" accept="image/jpeg,image/png,image/webp" (change)="selectEvidenceFile($event,'Signature','resubmission')"></label>
          <div class="actions"><button class="secondary compact" type="button" [disabled]="uploading() !== '' || !draft.selectedSignatureFile" (click)="uploadEvidence('Signature','resubmission')">Upload signature</button><span>{{ evidenceStatus(draft,'Signature') }}</span><button *ngIf="isScanPending(draft,'Signature')" class="secondary compact" type="button" [disabled]="uploading() !== ''" (click)="retryEvidenceScan('Signature','resubmission')">Retry scan</button></div>
          <label>Receiver photo file<input type="file" accept="image/jpeg,image/png,image/webp,application/pdf" (change)="selectEvidenceFile($event,'Photo','resubmission')"></label>
          <div class="actions"><button class="secondary compact" type="button" [disabled]="uploading() !== '' || !draft.selectedPhotoFile" (click)="uploadEvidence('Photo','resubmission')">Upload photo/evidence</button><span>{{ evidenceStatus(draft,'Photo') }}</span><button *ngIf="isScanPending(draft,'Photo')" class="secondary compact" type="button" [disabled]="uploading() !== ''" (click)="retryEvidenceScan('Photo','resubmission')">Retry scan</button></div>
          <progress *ngIf="uploading() === 'resubmission:Signature' || uploading() === 'resubmission:Photo'" [value]="uploadProgress()" max="100" aria-label="Evidence upload progress"></progress>
          <label>Resubmission reason<textarea [(ngModel)]="draft.reason" name="resubmitReason" minlength="3" maxlength="1000" required></textarea></label>
          <button class="primary" type="submit" [disabled]="saving() || uploading() !== '' || !hasCleanEvidence(draft)">Submit evidence for review</button>
        </form>
      </aside>
      <div class="backdrop" *ngIf="pourDraft" (click)="pourDraft = null"></div>
      <aside class="drawer" *ngIf="pourDraft" aria-label="Pour scheduling form">
        <header><div><p class="eyebrow">SCR-M08-02 · POUR</p><h2>Schedule delivery</h2></div>
          <button class="icon" type="button" (click)="pourDraft = null" aria-label="Close">×</button></header>
        <form class="form-section" (ngSubmit)="savePour()">
          <p class="notice">{{ pourDraft.customerNameSnapshot }} · {{ pourDraft.quantity }} · M06 schedule {{ pourDraft.scheduleId }}</p>
          <label>Plant ID<input [(ngModel)]="pourDraft.plantId" name="plantId" required></label>
          <div class="two"><label>Window starts<input [(ngModel)]="pourDraft.windowStart" name="windowStart" type="datetime-local" required></label>
            <label>Window ends<input [(ngModel)]="pourDraft.windowEnd" name="windowEnd" type="datetime-local" required></label></div>
          <label>Slump<input [(ngModel)]="pourDraft.slump" name="slump" maxlength="40"></label>
          <label>Placement<select [(ngModel)]="pourDraft.placementMethod" name="placement">
            <option>Pump</option><option>Boom</option><option>Crane</option><option>Direct</option>
          </select></label>
          <label>Pump type<input [(ngModel)]="pourDraft.pumpType" name="pumpType"></label>
          <label>Pump quantity<input [(ngModel)]="pourDraft.pumpQuantity" name="pumpQuantity" type="number" min="0" step="0.001"></label>
          <label>Special instructions<textarea [(ngModel)]="pourDraft.specialInstructions" name="instructions"></textarea></label>
          <button class="primary" type="submit" [disabled]="saving()">Confirm pour</button>
        </form>
      </aside>

      <div class="backdrop" *ngIf="planner()" (click)="planner.set(null)"></div>
      <aside class="drawer" *ngIf="planner()" aria-label="Trip planner">
        <header><div><p class="eyebrow">SCR-M08-04 · TRIP PLANNER</p><h2>Plan trips</h2></div>
          <button class="icon" type="button" (click)="planner.set(null)" aria-label="Close">×</button></header>
        <form class="form-section" (ngSubmit)="saveTrips()">
          <p class="notice">Use M03 vehicle capacity when splitting the pour. Server checks vehicle/driver overlap and compliance.</p>
          <label>Vehicle ID<input [(ngModel)]="tripDraft.vehicleId" name="vehicleId"></label>
          <label>Driver ID<input [(ngModel)]="tripDraft.driverId" name="driverId"></label>
          <label>Planned quantity<input [(ngModel)]="tripDraft.plannedQuantity" name="plannedQuantity" type="number" min="0.001" step="0.001" required></label>
          <label>Lot / heat number<input [(ngModel)]="tripDraft.lotOrHeatNumber" name="lot"></label>
          <label>MTC reference<input [(ngModel)]="tripDraft.mtcReference" name="mtc"></label>
          <button class="primary" type="submit" [disabled]="saving()">Save trip plan</button>
        </form>
      </aside>

      <div class="backdrop" *ngIf="actionDraft" (click)="actionDraft = null"></div>
      <aside class="drawer" *ngIf="actionDraft" aria-label="Trip action form">
        <header><div><p class="eyebrow">SCR-M08-05 · TRIP ACTION</p><h2>{{ actionDraft.action }} · {{ actionDraft.trip.number }}</h2></div>
          <button class="icon" type="button" (click)="actionDraft = null" aria-label="Close">×</button></header>
        <form class="form-section" (ngSubmit)="saveAction()">
          <label *ngIf="actionDraft.action === 'Loaded'">Loaded quantity<input [(ngModel)]="actionDraft.loadedQuantity" name="loadedQuantity" type="number" min="0.001" step="0.001" required></label>
          <label *ngIf="actionDraft.action === 'Loaded'">Batch slip number<input [(ngModel)]="actionDraft.batchSlipNumber" name="batchSlip" required></label>
          <label *ngIf="actionDraft.action === 'Completed'">Delivered quantity<input [(ngModel)]="actionDraft.deliveredQuantity" name="deliveredQuantity" type="number" min="0" step="0.001" required></label>
          <label *ngIf="actionDraft.action === 'Completed'">Returned quantity<input [(ngModel)]="actionDraft.returnedQuantity" name="returnedQuantity" type="number" min="0" step="0.001"></label>
          <ng-container *ngIf="actionDraft.action === 'Completed'">
            <label>Receiver name<input [(ngModel)]="actionDraft.receiverName" name="receiverName" maxlength="160" required></label>
            <label>Receiver mobile<input [(ngModel)]="actionDraft.receiverMobile" name="receiverMobile"></label>
            <label>Receiver signature file<input type="file" accept="image/jpeg,image/png,image/webp" (change)="selectEvidenceFile($event,'Signature','completion')"></label>
            <div class="actions"><button class="secondary compact" type="button" [disabled]="uploading() !== '' || !actionDraft.selectedSignatureFile" (click)="uploadEvidence('Signature','completion')">Upload signature</button><span>{{ evidenceStatus(actionDraft,'Signature') }}</span><button *ngIf="isScanPending(actionDraft,'Signature')" class="secondary compact" type="button" [disabled]="uploading() !== ''" (click)="retryEvidenceScan('Signature','completion')">Retry scan</button></div>
            <label>Receiver photo file<input type="file" accept="image/jpeg,image/png,image/webp,application/pdf" (change)="selectEvidenceFile($event,'Photo','completion')"></label>
            <div class="actions"><button class="secondary compact" type="button" [disabled]="uploading() !== '' || !actionDraft.selectedPhotoFile" (click)="uploadEvidence('Photo','completion')">Upload photo/evidence</button><span>{{ evidenceStatus(actionDraft,'Photo') }}</span><button *ngIf="isScanPending(actionDraft,'Photo')" class="secondary compact" type="button" [disabled]="uploading() !== ''" (click)="retryEvidenceScan('Photo','completion')">Retry scan</button></div>
            <progress *ngIf="uploading() === 'completion:Signature' || uploading() === 'completion:Photo'" [value]="uploadProgress()" max="100" aria-label="Evidence upload progress"></progress>
            <small class="muted">Uploads remain unverified until a supervisor reviews them. If scanning is unavailable, evidence stays quarantined and cannot be billed.</small>
          </ng-container>
          <button class="primary" type="submit" [disabled]="saving()">Save action</button>
        </form>
      </aside>

      <div class="backdrop" *ngIf="timeline()" (click)="timeline.set(null)"></div>
      <aside class="drawer" *ngIf="timeline()" aria-label="Pour trip timeline">
        <header><div><p class="eyebrow">SCR-M08-03 · POUR DETAIL</p><h2>{{ timeline().pour.customerNameSnapshot }}</h2></div>
          <button class="icon" type="button" (click)="timeline.set(null)" aria-label="Close">×</button></header>
        <p>Ordered/scheduled {{ timeline().pour.scheduledQuantity }} · Delivered {{ timeline().pour.deliveredQuantity }} · Returned {{ timeline().pour.returnedQuantity }}</p>
        <div class="line" *ngFor="let trip of timeline().trips">
          <strong>{{ trip.number }} · {{ trip.status }}</strong>
          <small>{{ trip.loadedAt | date:'short' }} → {{ trip.dispatchedAt | date:'short' }} → {{ trip.arrivedAt | date:'short' }} → {{ trip.completedAt | date:'short' }}</small>
          <small>Evidence: {{ trip.evidenceState }} · {{ trip.receiverName || 'Receiver not recorded' }}</small>
        </div>
      </aside>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1440px;margin:0 auto;color:var(--ink,#292723)}
    .page-head{display:flex;justify-content:space-between;align-items:flex-start;gap:24px;margin-bottom:20px}
    .eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}
    h1{font-size:30px;margin:4px 0}.muted,small{color:var(--muted,#77736c)}small{display:block;margin-top:4px}
    .head-actions,.actions,.two{display:flex;gap:10px;align-items:center}.two>*{flex:1}
    button{font:inherit;border:0;border-radius:6px;padding:10px 14px;cursor:pointer}
    input,select,textarea{font:inherit;padding:10px 12px;border:1px solid #d7d1c8;border-radius:6px;background:#fff;min-width:0}
    .primary{background:var(--copper,#a65d3b);color:white}.secondary{background:#eee9e1;color:#39352f}
    .compact{padding:7px 10px;font-size:12px}.notice{background:#f0e8de;padding:12px;border-radius:6px;font-size:13px;line-height:1.5}
    .error{color:#a3342a}.success{color:#24734f}.loading,.empty{padding:18px;color:#77736c}
    .metrics{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:18px 0}.metrics div,.panel,.pour{background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:16px}
    .metrics small{display:block}.metrics strong{font-size:22px}.panel{margin:16px 0}.panel>header{display:flex;justify-content:space-between}
    .card,.line{display:flex;align-items:center;justify-content:space-between;gap:14px;padding:12px;margin:10px 0;border:1px solid #e1dbd1;border-radius:7px;background:#fcfbf8}
    .pour{margin:12px 0}.pour-head{display:flex;justify-content:space-between;align-items:flex-start}.chip{background:#eee9e1;border-radius:20px;padding:4px 9px;font-size:11px}
    .backdrop{position:fixed;inset:0;background:#19171477;z-index:10}
    .drawer{position:fixed;right:0;top:0;bottom:0;width:min(620px,95vw);overflow:auto;background:#f7f4ee;padding:24px;z-index:11;box-shadow:-8px 0 30px #0002}
    .drawer header{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid #ddd6cc;padding-bottom:16px}
    .drawer h2{margin:4px 0}.icon{background:transparent;font-size:24px}.form-section{display:grid;gap:14px;padding:18px 0}
    label{display:grid;gap:6px;font-size:13px}.line{display:grid;justify-content:initial}
    @media(max-width:760px){.workspace{padding:18px 14px}.page-head{display:block}.head-actions{margin-top:14px}.metrics{grid-template-columns:repeat(2,1fr)}.card{align-items:flex-start;flex-direction:column}.two{flex-direction:column}}
  `],
})
export class M08Page implements OnInit, OnDestroy {
  private readonly api = inject(ApiClient);
  private readonly sanitizer = inject(DomSanitizer);
  readonly board = signal<any>({ unscheduled: [], pours: [], totals: {} });
  readonly pendingEvidence = signal<any[]>([]);
  readonly pendingError = signal('');
  readonly evidenceReview = signal<any | null>(null);
  readonly evidenceDraft = signal<any | null>(null);
  readonly decisionSaving = signal(false);
  readonly uploading = signal('');
  readonly uploadProgress = signal(0);
  readonly previewUrl = signal('');
  readonly previewMime = signal('');
  evidenceReason = '';
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  pourDraft: any | null = null;
  readonly planner = signal<any | null>(null);
  actionDraft: any | null = null;
  readonly timeline = signal<any | null>(null);
  date = new Date().toISOString().slice(0, 10);
  private refreshHandle?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.load();
    this.refreshHandle = setInterval(() => this.load(), 30_000);
  }

  ngOnDestroy(): void {
    if (this.refreshHandle) clearInterval(this.refreshHandle);
  }

  load(): void {
    this.loading.set(true);
    this.loadPendingEvidence();
    this.api.get<any>(`/m08/dispatch-board?date=${this.date}`).subscribe({
      next: (data) => { this.board.set(data); this.loading.set(false); },
      error: () => { this.error.set('Dispatch board unavailable. Check M08 activation and your role.'); this.loading.set(false); },
    });
  }

  loadPendingEvidence(): void {
    this.pendingError.set('');
    this.api.get<any>('/m08/evidence/pending').subscribe({
      next: (data) => this.pendingEvidence.set(data.items ?? []),
      error: (error) => {
        this.pendingEvidence.set([]);
        this.pendingError.set(error?.status === 403
          ? 'Evidence review requires the M08 evidence-review permission and branch scope.'
          : 'Pending evidence could not be loaded.');
      },
    });
  }

  private evidenceDraftFor(flow: 'completion' | 'resubmission'): any | null {
    return flow === 'completion' ? this.actionDraft : this.evidenceDraft();
  }

  selectEvidenceFile(event: Event, purpose: 'Signature' | 'Photo', flow: 'completion' | 'resubmission'): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const allowed = purpose === 'Signature'
      ? ['image/jpeg', 'image/png', 'image/webp']
      : ['image/jpeg', 'image/png', 'image/webp', 'application/pdf'];
    if (!allowed.includes(file.type) || file.size < 1 || file.size > 10 * 1024 * 1024) {
      this.error.set(purpose === 'Signature'
        ? 'Choose a non-empty JPEG, PNG or WebP image up to 10 MB for the receiver signature.'
        : 'Choose a non-empty JPEG, PNG, WebP or PDF file up to 10 MB for delivery evidence.');
      input.value = '';
      return;
    }
    const draft = this.evidenceDraftFor(flow);
    if (!draft) return;
    draft[`selected${purpose}File`] = file;
    this.error.set('');
  }

  private loadPendingScans(draft: any): void {
    this.api.get<any>(`/m08/trips/${draft.trip.tripId}/evidence-assets/pending`).subscribe({
      next: (data) => {
        draft.evidenceUploads ??= {};
        for (const upload of data.items ?? []) draft.evidenceUploads[upload.purpose] = upload;
      },
      error: () => this.error.set('Pending malware scans could not be restored. Refresh and check delivery access.'),
    });
  }
  uploadEvidence(purpose: 'Signature' | 'Photo', flow: 'completion' | 'resubmission'): void {
    const draft = this.evidenceDraftFor(flow);
    const file = draft?.[`selected${purpose}File`] as File | undefined;
    if (!draft || !file) return;
    this.uploading.set(`${flow}:${purpose}`);
    this.uploadProgress.set(0);
    const tripId = draft.trip.tripId;
    const path = `/m08/trips/${tripId}/evidence-assets?purpose=${purpose}&fileName=${encodeURIComponent(file.name)}`;
    this.api.upload<any>(path, file).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress) {
          const total = event.total ?? file.size;
          this.uploadProgress.set(total ? Math.round(event.loaded * 100 / total) : 0);
        } else if (event.type === HttpEventType.Response && event.body) {
          const result = event.body as any;
          draft.evidenceUploads ??= {};
          draft.evidenceUploads[purpose] = result;
          draft[`selected${purpose}File`] = null;
          this.uploading.set('');
          if (result.scanStatus === 'Clean' && result.isCurrent !== false) {
            draft.trip.version++;
            draft.trip[purpose === 'Signature' ? 'signatureReference' : 'photoReference'] = result.documentId;
            this.notice.set(`${purpose} evidence uploaded and malware scan passed. It is still unverified until supervisor review.`);
            this.load();
            window.dispatchEvent(new CustomEvent('m08:evidence-updated'));
          } else if (result.scanStatus === 'Clean') {
            this.notice.set('This version passed scanning but a newer upload already superseded it. The trip continues to use its latest evidence version.');
          } else {
            this.notice.set(result.detail || 'Evidence is quarantined; retry its scan when the scanner is available. It cannot be previewed or billed yet.');
          }
        }
      },
      error: (error) => {
        this.uploading.set('');
        const pending = error?.error?.documentId ? error.error : null;
        const detail = error?.error?.detail ?? error?.error?.title ?? 'Evidence upload failed. Select the file again and retry.';
        draft.evidenceUploads ??= {};
        draft.evidenceUploads[purpose] = pending ?? { scanStatus: error?.status === 422 ? 'Rejected' : 'Failed', detail, storageStatus: error?.status === 422 ? 'Deleted' : undefined };
        if (pending) draft[`selected${purpose}File`] = null;
        this.error.set(detail);
      },
    });
  }

  retryEvidenceScan(purpose: 'Signature' | 'Photo', flow: 'completion' | 'resubmission'): void {
    const draft = this.evidenceDraftFor(flow);
    const upload = draft?.evidenceUploads?.[purpose];
    if (!draft || !upload?.documentId) return;
    this.uploading.set(`${flow}:${purpose}`);
    this.api.post<any, any>(`/m08/trips/${draft.trip.tripId}/evidence-assets/${upload.documentId}/scan-retry`, {}).subscribe({
      next: (result) => {
        draft.evidenceUploads[purpose] = result;
        this.uploading.set('');
        if (result.scanStatus === 'Clean' && result.isCurrent !== false) {
          draft.trip.version++;
          draft.trip[purpose === 'Signature' ? 'signatureReference' : 'photoReference'] = result.documentId;
          this.notice.set(`${purpose} evidence scan passed. The delivery remains unverified until supervisor review.`);
          this.load();
          window.dispatchEvent(new CustomEvent('m08:evidence-updated'));
        } else {
          this.notice.set(result.scanStatus === 'Clean' ? 'This version passed scanning but a newer upload already superseded it. The trip continues to use its latest evidence version.' : result.detail || 'The file remains quarantined. No preview or billing is allowed until a clean scan succeeds.');
        }
      },
      error: (error) => {
        this.uploading.set('');
        if (error?.error?.documentId) draft.evidenceUploads[purpose] = error.error;
        else if (error?.status === 422) draft.evidenceUploads[purpose] = { ...upload, scanStatus: 'Rejected', storageStatus: 'Deleted', detail: error?.error?.detail ?? 'Malware was detected.' };
        this.error.set(error?.error?.detail ?? 'The malware scan could not be retried. The upload remains quarantined.');
      },
    });
  }

  evidenceStatus(draft: any, purpose: 'Signature' | 'Photo'): string {
    const upload = draft?.evidenceUploads?.[purpose];
    const selected = draft?.[`selected${purpose}File`] as File | undefined;
    if (selected) return `Selected ${selected.name}; upload it to replace the current evidence.`;
    if (upload?.scanStatus === 'Clean' && upload.isCurrent === false) return 'Clean scan passed, but a newer upload superseded this version.';
    if (upload?.scanStatus === 'Clean') return `Clean scan passed (version ${upload.versionNumber})`;
    if (upload?.scanStatus === 'Pending') return `Quarantined - ${upload.detail || 'scanner unavailable; retry scan'}`;
    if (upload?.scanStatus === 'Rejected') return `Rejected - ${upload.detail || 'select another file'}`;
    if (upload?.detail) return upload.detail;
    return 'No file selected';
  }
  isScanPending(draft: any, purpose: 'Signature' | 'Photo'): boolean {
    return draft?.evidenceUploads?.[purpose]?.scanStatus === 'Pending';
  }

  hasCleanEvidence(draft: any): boolean {
    return ['Signature', 'Photo'].some((purpose) => draft?.evidenceUploads?.[purpose]?.scanStatus === 'Clean' && draft?.evidenceUploads?.[purpose]?.isCurrent !== false);
  }

  previewEvidence(asset: any): void {
    const path = String(asset.previewPath ?? '').replace(/^\/api\/v1/, '');
    if (!path) return;
    this.releasePreview();
    this.api.getBlob(path).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        this.previewMime.set(asset.contentType);
        this.previewUrl.set(url);
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Authorized evidence preview failed.'),
    });
  }

  safePreviewUrl(): SafeResourceUrl | null {
    const url = this.previewUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  }

  downloadEvidence(asset: any): void {
    const path = String(asset.previewPath ?? '').replace(/^\/api\/v1/, '').replace(/\/content$/, '/download');
    if (!path) return;
    this.api.getBlob(path).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = asset.fileName || 'delivery-evidence';
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 30_000);
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Authorized evidence download failed.'),
    });
  }

  closeEvidenceReview(): void {
    this.releasePreview();
    this.evidenceReview.set(null);
  }

  private releasePreview(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
    this.previewUrl.set('');
    this.previewMime.set('');
  }
  openEvidenceReview(item: any): void {
    this.evidenceReason = '';
    this.api.get<any>(`/m08/trips/${item.tripId}/evidence-review`).subscribe({
      next: (data) => this.evidenceReview.set(data),
      error: (error) => this.error.set(error?.error?.detail ?? 'Evidence review could not be opened.'),
    });
  }

  decideEvidence(decision: 'Verify' | 'Reject'): void {
    const item = this.evidenceReview();
    if (!item || !this.evidenceReason.trim()) return;
    if (decision === 'Verify' && !item.canVerify) {
      this.error.set(item.attachmentBlocker ?? 'Secure M18 evidence is not available for verification.');
      return;
    }
    this.decisionSaving.set(true);
    this.api.post<any, any>(`/m08/trips/${item.tripId}/evidence-review`, {
      version: item.version, decision, reason: this.evidenceReason.trim(),
    }).subscribe({
      next: (result) => {
        this.decisionSaving.set(false); this.closeEvidenceReview();
        this.notice.set(result.evidenceState === 'Verified'
          ? 'Receiver evidence verified. Refresh the M09 challan worklist and M10 eligible-challan list to load the persisted result.'
          : 'Evidence rejected with reason. Delivery remains blocked from billing until resubmitted and reviewed.');
        this.load();
        window.dispatchEvent(new CustomEvent('m08:evidence-updated'));
      },
      error: (error) => {
        this.decisionSaving.set(false);
        this.error.set(error?.error?.detail ?? 'Evidence decision failed; reload and check the trip version.');
      },
    });
  }

  openResubmission(trip: any): void {
    const draft = {
      trip, receiverName: trip.receiverName ?? '', receiverMobile: trip.receiverMobile ?? '',
      signatureReference: trip.signatureReference ?? '', photoReference: trip.photoReference ?? '', reason: '',
      evidenceUploads: {}, selectedSignatureFile: null, selectedPhotoFile: null,
    };
    this.evidenceDraft.set(draft);
    this.loadPendingScans(draft);
  }
  submitEvidenceResubmission(): void {
    const draft = this.evidenceDraft();
    if (!draft) return;
    if (!draft.receiverName.trim() || !draft.reason.trim() || !this.hasCleanEvidence(draft)) {
      this.error.set('Enter receiver name and a reason, then upload at least one file with a clean malware scan.');
      return;
    }
    this.saving.set(true);
    this.api.post<any, any>(`/m08/trips/${draft.trip.tripId}/evidence`, {
      version: draft.trip.version, receiverName: draft.receiverName.trim(),
      receiverMobile: draft.receiverMobile.trim() || null, signatureReference: null,
      photoReference: null, reason: draft.reason.trim(),
    }).subscribe({
      next: () => {
        this.saving.set(false); this.evidenceDraft.set(null);
        this.notice.set('Evidence resubmitted. It remains unverified until an authorized supervisor reviews it.');
        this.load();
        window.dispatchEvent(new CustomEvent('m08:evidence-updated'));
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Evidence resubmission failed.'); },
    });
  }
  openPour(schedule: any): void {
    this.pourDraft = {
      scheduleId: schedule.scheduleId, customerNameSnapshot: schedule.customerNameSnapshot,
      quantity: schedule.quantity, plantId: schedule.preferredPlantId || '',
      windowStart: `${this.date}T08:00`, windowEnd: `${this.date}T09:00`,
      slump: '', placementMethod: 'Direct', pumpType: '', pumpQuantity: 0,
      customerEngineer: '', coordinator: '', specialInstructions: '',
    };
  }

  savePour(): void {
    const form = this.pourDraft;
    if (!form) return;
    this.saving.set(true);
    const body = {
      ...form,
      plantId: form.plantId || null,
      windowStart: new Date(form.windowStart).toISOString(),
      windowEnd: new Date(form.windowEnd).toISOString(),
      pumpQuantity: Number(form.pumpQuantity || 0),
    };
    this.api.post<any, any>('/m08/pours', body).subscribe({
      next: (result) => {
        this.saving.set(false); this.pourDraft = null;
        this.notice.set(result.capacityWarning
          ? 'Pour saved with a plant capacity warning. External notification is unavailable.'
          : 'Pour scheduled. External notification is unavailable.');
        this.load();
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Pour could not be scheduled.'); },
    });
  }

  openPlanner(pour: any): void {
    this.planner.set(pour);
    this.tripDraft = {
      vehicleId: '', driverId: '', plannedQuantity: pour.scheduledQuantity - pour.deliveredQuantity,
      lotOrHeatNumber: '', mtcReference: '',
    };
  }

  tripDraft: any = {};
  saveTrips(): void {
    const pour = this.planner();
    if (!pour) return;
    this.saving.set(true);
    this.api.post<any, any>(`/m08/pours/${pour.pourId}/trips`, {
      version: pour.version,
      trips: [{
        vehicleId: this.tripDraft.vehicleId || null,
        driverId: this.tripDraft.driverId || null,
        plannedQuantity: Number(this.tripDraft.plannedQuantity),
        lotOrHeatNumber: this.tripDraft.lotOrHeatNumber || null,
        mtcReference: this.tripDraft.mtcReference || null,
      }],
    }).subscribe({
      next: () => { this.saving.set(false); this.planner.set(null); this.notice.set('Trip plan saved.'); this.load(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Trip plan failed validation.'); },
    });
  }

  act(trip: any, action: string): void {
    this.actionDraft = {
      trip, action, loadedQuantity: trip.plannedQuantity,
      deliveredQuantity: trip.loadedQuantity, returnedQuantity: 0,
      batchSlipNumber: '', receiverName: '', receiverMobile: '',
      signatureReference: '', photoReference: '', evidenceUploads: {}, selectedSignatureFile: null, selectedPhotoFile: null,
    };
    this.loadPendingScans(this.actionDraft);
  }
  saveAction(): void {
    const form = this.actionDraft;
    if (!form) return;
    if (form.action === 'Completed' && !form.receiverName.trim()) {
      this.error.set('Receiver name is required to complete the delivery.');
      return;
    }
    this.saving.set(true);
    this.api.post<any, any>(`/m08/trips/${form.trip.tripId}/actions`, {
      actionId: crypto.randomUUID(), version: form.trip.version, action: form.action,
      deviceAt: new Date().toISOString(),
      loadedQuantity: form.action === 'Loaded' ? Number(form.loadedQuantity) : null,
      deliveredQuantity: form.action === 'Completed' ? Number(form.deliveredQuantity) : null,
      returnedQuantity: form.action === 'Completed' ? Number(form.returnedQuantity || 0) : null,
      batchSlipNumber: form.batchSlipNumber || null,
      receiverName: form.receiverName || null, receiverMobile: form.receiverMobile || null,
      signatureReference: null, photoReference: null,
      exceptionType: null, exceptionReason: null,
    }).subscribe({
      next: (result) => {
        this.saving.set(false); this.actionDraft = null;
        this.notice.set(result.billingEligibility || `Trip ${result.status}.`);
        this.load();
        window.dispatchEvent(new CustomEvent('m08:evidence-updated'));
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Trip action failed.'); },
    });
  }

  openDetail(pour: any): void {
    this.api.get<any>(`/m08/pours/${pour.pourId}`).subscribe({
      next: (data) => this.timeline.set(data),
      error: () => this.error.set('Trip timeline is unavailable.'),
    });
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

type TrialBalanceRow = { ledgerCode: string; debit: number; credit: number };
type TrialBalanceResponse = {
  balances: TrialBalanceRow[];
  totalDebit: number;
  totalCredit: number;
};
@Component({
  selector: 'app-m12-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head"><div><p class="eyebrow">FINANCE · M12 BOOKS</p><h1>Accounting</h1>
        <p class="muted">Double-entry voucher engine, chart of accounts and trial balance.</p></div>
        <button class="primary" type="button" (click)="openJournal()">Manual journal</button></header>
      <p class="notice">M10 invoice and M11 supported receipt postings now use the shared M12 voucher engine. Configure branch/GSTIN mappings first; procurement, tax and expense system flows remain pending.</p>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p><p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <nav class="tabs"><button [class.active]="tab === 'trial'" type="button" (click)="tab='trial'; loadTrialBalance()">Trial balance</button>
        <button [class.active]="tab === 'vouchers'" type="button" (click)="tab='vouchers'; loadVouchers()">Vouchers</button>
        <button [class.active]="tab === 'accounts'" type="button" (click)="tab='accounts'; loadAccounts()">Chart of accounts</button>
        <button [class.active]="tab === 'mappings'" type="button" (click)="tab='mappings'; loadMappings()">Ledger mappings</button></nav>
      <section class="panel" *ngIf="tab === 'trial'">
        <header><h2>Trial balance</h2><button class="secondary" type="button" (click)="loadTrialBalance()">Refresh</button></header>
        <p class="empty" *ngIf="!balances().length">No posted voucher balances exist yet.</p>
        <table *ngIf="balances().length"><thead><tr><th>Ledger</th><th>Debit</th><th>Credit</th></tr></thead>
          <tbody><tr *ngFor="let row of balances()"><td>{{ row.ledgerCode }}</td><td>{{ row.debit | currency:'INR':'symbol':'1.2-2' }}</td><td>{{ row.credit | currency:'INR':'symbol':'1.2-2' }}</td></tr></tbody>
          <tfoot><tr><th>Total</th><th>{{ totalDebit() | currency:'INR':'symbol':'1.2-2' }}</th><th>{{ totalCredit() | currency:'INR':'symbol':'1.2-2' }}</th></tr></tfoot></table>
      </section>
      <section class="panel" *ngIf="tab === 'vouchers'">
        <header><h2>Posted vouchers</h2><button class="secondary" type="button" (click)="loadVouchers()">Refresh</button></header>
        <p class="empty" *ngIf="!vouchers().length">No posted vouchers.</p>
        <table *ngIf="vouchers().length"><thead><tr><th>Number</th><th>Date</th><th>Type</th><th>Source</th><th>Debit</th><th>Credit</th></tr></thead>
          <tbody><tr *ngFor="let row of vouchers()"><td>{{ row.number }}</td><td>{{ row.voucherDate | date:'mediumDate' }}</td>
            <td>{{ row.voucherType }}</td><td>{{ row.sourceModule }}</td><td>{{ row.totalDebit | currency:'INR':'symbol':'1.2-2' }}</td><td>{{ row.totalCredit | currency:'INR':'symbol':'1.2-2' }}</td></tr></tbody></table>
      </section>
      <section class="panel" *ngIf="tab === 'accounts'">
        <header><h2>Configured accounts</h2><button class="secondary" type="button" (click)="loadAccounts()">Refresh</button></header>
        <p class="notice">Tenant ledger accounts are stored in the existing onboarding chart and audited on change. Used accounts cannot be deactivated; mappings require compatible account groups.</p>
        <form class="mapping-form" (ngSubmit)="createAccount()">
          <label>Account code<input [(ngModel)]="ledgerForm.code" name="ledgerCode" required maxlength="64"></label>
          <label>Account name<input [(ngModel)]="ledgerForm.name" name="ledgerName" required maxlength="120"></label>
          <label>Account group<select [(ngModel)]="ledgerForm.group" name="ledgerGroup" required><option *ngFor="let group of accountGroups" [value]="group">{{ group }}</option></select></label>
          <button class="primary" type="submit" [disabled]="saving()">Add ledger account</button>
        </form>
        <table *ngIf="accounts().length"><thead><tr><th>Code</th><th>Account</th><th>Group</th><th>Status</th><th></th></tr></thead>
          <tbody><tr *ngFor="let row of accounts()"><td>{{ row.code }}</td><td>{{ row.name }}</td><td>{{ row.group }}</td><td>{{ row.isActive ? 'Active' : 'Inactive' }}</td><td><button *ngIf="!row.isSystem" class="link" type="button" (click)="renameAccount(row)">Rename</button> <button *ngIf="row.isActive && !row.isSystem" class="link" type="button" (click)="deactivateAccount(row)">Deactivate</button></td></tr></tbody></table>
      </section>
      <section class="panel" *ngIf="tab === 'mappings'">
        <header><div><h2>Tenant accounting mappings</h2><p class="muted">Mappings are branch/GSTIN scoped, effective dated and audited. Posting remains fail-closed when a required active mapping is absent.</p></div><button class="secondary" type="button" (click)="loadMappings()">Refresh</button></header>
        <form class="mapping-form" (ngSubmit)="createMapping()">
          <label>Branch<select [(ngModel)]="mappingForm.branchId" name="mappingBranch" required><option value="">Choose branch</option><option *ngFor="let branch of mappingData()?.branches" [value]="branch.id">{{ branch.name }} · {{ branch.gstin }}</option></select></label>
          <label>Posting role<select [(ngModel)]="mappingForm.mappingCode" name="mappingCode" required><option *ngFor="let code of mappingData()?.codes" [value]="code">{{ code }}</option></select></label>
          <label>Ledger account<select [(ngModel)]="mappingForm.ledgerCode" name="mappingLedger" required><option value="">Choose active account</option><option *ngFor="let account of mappingData()?.accounts" [value]="account.code">{{ account.code }} · {{ account.name }} ({{ account.group }})</option></select></label>
          <label>Effective from<input [(ngModel)]="mappingForm.effectiveFrom" name="mappingFrom" type="date" required></label>
          <label>Effective to (optional)<input [(ngModel)]="mappingForm.effectiveTo" name="mappingTo" type="date"></label>
          <button class="primary" type="submit" [disabled]="saving()">Save mapping</button>
        </form>
        <p class="empty" *ngIf="!mappingData()?.items?.length">No tenant mappings configured. Invoice and receipt posting will reject missing mappings.</p>
        <div class="table-wrap" *ngIf="mappingData()?.items?.length"><table><thead><tr><th>Branch / GSTIN</th><th>Role</th><th>Ledger</th><th>Effective range</th><th>Status</th><th></th></tr></thead>
          <tbody><tr *ngFor="let row of mappingData()?.items"><td>{{ branchName(row.branchId) }}<small>{{ row.gstin }}</small></td><td>{{ row.mappingCode }}</td><td>{{ row.ledgerCode }}</td><td>{{ row.effectiveFrom }} – {{ row.effectiveTo || 'open' }}</td><td>{{ row.isActive ? 'Active' : 'Inactive' }}</td><td><button *ngIf="row.isActive" class="link" type="button" (click)="deactivateMapping(row)">Deactivate</button></td></tr></tbody></table></div>
      </section>
      <div class="backdrop" *ngIf="journalOpen()" (click)="journalOpen.set(false)"></div>
      <aside class="drawer" *ngIf="journalOpen()" aria-label="Manual voucher form">
        <header><div><p class="eyebrow">SCR-M12-03 · MANUAL VOUCHER</p><h2>Journal / Contra</h2></div>
          <button class="icon" type="button" (click)="journalOpen.set(false)" aria-label="Close">×</button></header>
        <form class="form" (ngSubmit)="postJournal()">
          <label>Voucher type<select [(ngModel)]="form.voucherType" name="type"><option>Journal</option><option>Contra</option></select></label>
          <label>Branch ID<input [(ngModel)]="form.branchId" name="branch" required></label>
          <label>Date<input [(ngModel)]="form.voucherDate" name="date" type="date" required></label>
          <label>Narration<input [(ngModel)]="form.narration" name="narration" required></label>
          <div class="entry" *ngFor="let line of lines; let i=index">
            <div class="entry-head"><strong>Line {{ i+1 }}</strong><button *ngIf="lines.length > 2" class="link" type="button" (click)="lines.splice(i,1)">Remove</button></div>
            <label>Ledger account<select [(ngModel)]="line.ledgerCode" [name]="'ledger-' + i" required>
              <option value="">Choose active ledger</option>
              <option *ngFor="let account of activeAccounts()" [value]="account.code">{{ account.code }} · {{ account.name }}</option>
            </select></label>
            <div class="two"><label>Debit<input [(ngModel)]="line.debit" [name]="'debit-' + i" type="number" min="0" step="0.01"></label>
              <label>Credit<input [(ngModel)]="line.credit" [name]="'credit-' + i" type="number" min="0" step="0.01"></label></div>
            <label>Party ID, if control account<input [(ngModel)]="line.partyId" [name]="'party-' + i"></label>
            <label>Line narration<input [(ngModel)]="line.narration" [name]="'line-note-' + i"></label>
          </div>
          <button class="secondary" type="button" (click)="addLine()">Add line</button>
          <p class="notice">Debit total {{ debitTotal() | currency:'INR':'symbol':'1.2-2' }} · Credit total {{ creditTotal() | currency:'INR':'symbol':'1.2-2' }}. Server enforces exact balance, ledger membership, party dimensions and period lock.</p>
          <button class="primary" type="submit" [disabled]="saving()">Post balanced voucher</button>
        </form>
      </aside>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1450px;margin:auto;color:var(--ink,#292723)}
    .page-head{display:flex;justify-content:space-between;align-items:flex-start}.eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}
    h1{font-size:30px;margin:4px 0}.muted,small{color:#77736c}.primary,.secondary{border:0;border-radius:6px;padding:10px 14px;cursor:pointer}
    .primary{background:#a65d3b;color:#fff}.secondary{background:#eee9e1;color:#39352f}.notice{background:#f0e8de;padding:12px;border-radius:6px;font-size:13px;line-height:1.5}
    .error{color:#a3342a}.success{color:#24734f}.tabs{display:flex;gap:6px;border-bottom:1px solid #d9d3ca;margin:18px 0}
    .tabs button{border:0;background:#eee9e1;padding:9px 12px;border-radius:6px}.tabs .active{background:#a65d3b;color:#fff}
    .panel{background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:16px}.panel header{display:flex;justify-content:space-between;align-items:center}
    table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:11px;border-bottom:1px solid #eee9e1;font-size:13px}
    th{color:#77736c}.empty{padding:18px;color:#77736c}.backdrop{position:fixed;inset:0;background:#19171477;z-index:10}
    .drawer{position:fixed;right:0;top:0;bottom:0;width:min(700px,96vw);overflow:auto;background:#f7f4ee;padding:24px;z-index:11}
    .drawer header{display:flex;justify-content:space-between;border-bottom:1px solid #ddd6cc;padding-bottom:16px}.drawer h2{margin:4px 0}
    .icon{border:0;background:none;font-size:24px;cursor:pointer}.form{display:grid;gap:12px;padding:18px 0}.form label,.mapping-form label{display:grid;gap:6px;font-size:13px}.mapping-form{display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:12px;margin:18px 0}.mapping-form label{font-size:13px}.link{border:0;background:none;color:#9d392e;cursor:pointer}
    input,select{font:inherit;padding:9px 11px;border:1px solid #d7d1c8;border-radius:6px}.entry{padding:12px;background:#fff;border:1px solid #e3ded6;border-radius:7px}
    .entry-head,.two{display:flex;justify-content:space-between;gap:10px}.two>*{flex:1}.link{border:0;color:#a3342a;background:none}
    @media(max-width:740px){.workspace{padding:18px 14px}.page-head{align-items:center}.two{flex-direction:column}}
  `],
})
export class M12Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly accounts = signal<any[]>([]);
  readonly ledgerVersion = signal(0);
  readonly mappingData = signal<any | null>(null);
  readonly vouchers = signal<any[]>([]);
  readonly balances = signal<TrialBalanceRow[]>([]);
  private readonly trialBalanceTotals = signal<Pick<TrialBalanceResponse, 'totalDebit' | 'totalCredit'> | null>(null);
  readonly journalOpen = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  tab = 'trial';
  form: any = {};
  mappingForm: any = {};
  ledgerForm: any = { code: "", name: "", group: "Assets" };
  accountGroups = ["Assets", "Liabilities", "Equity", "Income", "Expenses"];
  lines: any[] = [];

  ngOnInit(): void { this.resetForm(); this.loadTrialBalance(); this.resetMappingForm(); }
  loadMappings(): void {
    this.error.set('');
    this.api.get<any>('/m12/ledger-mappings').subscribe({
      next: (data) => this.mappingData.set(data),
      error: (error) => this.error.set(error?.error?.detail ?? 'Ledger mappings unavailable; confirm M12 and mapping-view permission.'),
    });
  }
  private resetMappingForm(): void {
    this.mappingForm = { mappingCode: 'Receivables', branchId: '', ledgerCode: '',
      effectiveFrom: new Date().toISOString().slice(0, 10), effectiveTo: '' };
  }
  branchName(branchId: string): string {
    return this.mappingData()?.branches?.find((branch: any) => branch.id === branchId)?.name ?? branchId;
  }
  createMapping(): void {
    if (!this.mappingForm.branchId || !this.mappingForm.mappingCode || !this.mappingForm.ledgerCode || !this.mappingForm.effectiveFrom ||
        (this.mappingForm.effectiveTo && this.mappingForm.effectiveTo < this.mappingForm.effectiveFrom)) {
      this.error.set('Choose a branch, posting role, active account and valid effective dates.'); return;
    }
    this.saving.set(true);
    this.api.post<any, any>('/m12/ledger-mappings', {
      ...this.mappingForm, effectiveTo: this.mappingForm.effectiveTo || null,
    }).subscribe({
      next: () => { this.saving.set(false); this.notice.set('Effective-dated ledger mapping saved and audited.'); this.resetMappingForm(); this.loadMappings(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Ledger mapping could not be saved.'); },
    });
  }
  deactivateMapping(row: any): void {
    const reason = window.prompt('Reason for deactivating this mapping?');
    if (!reason?.trim()) return;
    this.saving.set(true);
    this.api.post<any, any>(`/m12/ledger-mappings/${row.mappingId}/deactivate`, { version: row.version, reason }).subscribe({
      next: () => { this.saving.set(false); this.notice.set('Mapping deactivated and audit recorded.'); this.loadMappings(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Mapping could not be deactivated.'); },
    });
  }
  loadAccounts(): void {
    this.api.get<any>('/m12/accounts').subscribe({
      next: (data) => { this.accounts.set(data.items); this.ledgerVersion.set(data.version); },
      error: () => this.error.set('Chart of accounts unavailable.'),
    });
  }
  activeAccounts(): any[] { return this.accounts().filter(account => account.isActive); }
  openJournal(): void {
    this.resetForm();
    this.journalOpen.set(true);
    this.loadAccounts();
  }
  createAccount(): void {
    if (!/^[A-Za-z0-9][A-Za-z0-9_-]{1,63}$/.test(this.ledgerForm.code.trim()) ||
        this.ledgerForm.name.trim().length < 2 || !this.accountGroups.includes(this.ledgerForm.group)) {
      this.error.set('Enter a valid account code, name and group.'); return;
    }
    this.saving.set(true);
    this.api.post<any, any>('/m12/accounts', { ...this.ledgerForm, version: this.ledgerVersion() }).subscribe({
      next: () => { this.saving.set(false); this.notice.set('Tenant ledger account added and audited.'); this.ledgerForm = { code: '', name: '', group: 'Assets' }; this.loadAccounts(); this.loadMappings(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Ledger account could not be added.'); },
    });
  }
  renameAccount(row: any): void {
    const name = window.prompt('New account name', row.name);
    if (!name?.trim() || name.trim() === row.name) return;
    this.saving.set(true);
    this.api.put<any, any>(`/m12/accounts/${encodeURIComponent(row.code)}`, {
      version: this.ledgerVersion(), name,
    }).subscribe({
      next: () => { this.saving.set(false); this.notice.set('Tenant ledger account renamed and audited.'); this.loadAccounts(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Ledger account could not be renamed.'); },
    });
  }
  deactivateAccount(row: any): void {
    const reason = window.prompt('Reason for deactivating this account?');
    if (!reason?.trim()) return;
    this.saving.set(true);
    this.api.post<any, any>(`/m12/accounts/${encodeURIComponent(row.code)}/deactivate`, {
      version: this.ledgerVersion(), reason,
    }).subscribe({
      next: () => { this.saving.set(false); this.notice.set('Ledger account deactivated and audited.'); this.loadAccounts(); this.loadMappings(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Ledger account could not be deactivated.'); },
    });
  }
  loadVouchers(): void {
    this.api.get<any>('/m12/vouchers').subscribe({
      next: (data) => this.vouchers.set(data.items), error: () => this.error.set('Voucher list unavailable.'),
    });
  }
  totalDebit(): number { return this.trialBalanceTotals()?.totalDebit ?? 0; }
  totalCredit(): number { return this.trialBalanceTotals()?.totalCredit ?? 0; }

  loadTrialBalance(): void {
    this.error.set('');
    this.api.get<TrialBalanceResponse>('/m12/trial-balance').subscribe({
      next: (data) => {
        this.balances.set(data.balances);
        this.trialBalanceTotals.set({ totalDebit: data.totalDebit, totalCredit: data.totalCredit });
      },
      error: () => {
        this.balances.set([]);
        this.trialBalanceTotals.set(null);
        this.error.set('Trial balance unavailable.');
      },
    });
  }
  addLine(): void { this.lines.push({ ledgerCode: '', partyId: '', debit: 0, credit: 0, narration: '' }); }
  debitTotal(): number { return this.lines.reduce((sum, line) => sum + Number(line.debit || 0), 0); }
  creditTotal(): number { return this.lines.reduce((sum, line) => sum + Number(line.credit || 0), 0); }
  private resetForm(): void {
    this.form = { voucherType: 'Journal', branchId: '', voucherDate: new Date().toISOString().slice(0, 10), narration: '' };
    this.lines = [
      { ledgerCode: '', partyId: '', debit: 0, credit: 0, narration: '' },
      { ledgerCode: '', partyId: '', debit: 0, credit: 0, narration: '' },
    ];
  }
  postJournal(): void {
    this.saving.set(true);
    this.api.post<any, any>('/m12/vouchers/manual', {
      ...this.form, idempotencyKey: crypto.randomUUID(),
      lines: this.lines.map(x => ({
        ...x, partyId: x.partyId || null,
        branchId: this.form.branchId, siteId: null, salesOrderId: null, tripId: null,
      })),
    }).subscribe({
      next: (voucher) => {
        this.saving.set(false); this.journalOpen.set(false); this.notice.set(`Voucher ${voucher.number} posted.`);
        this.loadTrialBalance(); this.loadVouchers();
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Voucher could not be posted.'); },
    });
  }
}

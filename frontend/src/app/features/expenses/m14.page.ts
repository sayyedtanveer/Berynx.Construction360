import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m14-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head"><div><p class="eyebrow">FINANCE · M14 COST CAPTURE</p><h1>Expenses</h1>
        <p class="muted">Direct and indirect cost records, with M02 approval and M12 posting where configured.</p></div>
        <button class="secondary" type="button" (click)="load()">Refresh</button></header>
      <p class="notice">Attachment upload, reimbursements, petty cash, recurring costs, ITC/RCM, TDS and other-income workflows are not yet wired. Use this page only for supported non-RCM expense records.</p>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p><p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <section class="panel"><p class="eyebrow">TENANT CONFIGURATION</p><h2>Expense category</h2>
        <div class="form compact">
          <label>Name<input [(ngModel)]="categoryDraft.name"></label>
          <label>Type<select [(ngModel)]="categoryDraft.type"><option>Direct</option><option>Indirect</option><option>Capital</option><option>Other income</option></select></label>
          <label>Approval threshold<input type="number" min="0" step="0.01" [(ngModel)]="categoryDraft.approvalThreshold"></label>
          <label>Expense ledger code<input [(ngModel)]="categoryDraft.expenseLedgerCode"></label>
        </div>
        <button class="secondary" type="button" (click)="createCategory()">Add category</button>
      </section>
      <section class="panel">
        <p class="eyebrow">QUICK CAPTURE</p><h2>New expense</h2>
        <div class="form">
          <label>Date<input type="date" [(ngModel)]="draft.expenseDate"></label>
          <label>Category<select [(ngModel)]="draft.categoryId"><option value="">Choose category</option>
            <option *ngFor="let category of categories()" [value]="category.categoryId">{{ category.name }} · {{ category.type }}</option>
          </select></label>
          <label>Amount<input type="number" min="0.01" step="0.01" [(ngModel)]="draft.amount"></label>
          <label>Paid from ledger<input [(ngModel)]="draft.paidFromLedgerCode" placeholder="Configured ledger code"></label>
          <label class="wide">Description<input [(ngModel)]="draft.description"></label>
          <label>Branch ID<input [(ngModel)]="draft.branchId"></label>
          <label>Supplier bill number<input [(ngModel)]="draft.supplierBillNumber"></label>
          <label>Supplier GSTIN<input [(ngModel)]="draft.supplierGstin"></label>
          <label>GST amount<input type="number" min="0" step="0.01" [(ngModel)]="draft.gstAmount"></label>
          <label class="check"><input type="checkbox" [(ngModel)]="draft.employeePaid"> Employee paid</label>
          <label class="check"><input type="checkbox" [(ngModel)]="draft.itcEligible"> ITC eligible (posting remains blocked)</label>
          <label>Site ID<input [(ngModel)]="draft.siteId"></label><label>Sales order ID<input [(ngModel)]="draft.salesOrderId"></label>
          <label>Trip ID<input [(ngModel)]="draft.tripId"></label><label>Attachment reference<input [(ngModel)]="draft.attachmentReference"></label>
        </div>
        <button class="primary" type="button" [disabled]="saving()" (click)="create()">Save expense</button>
      </section>
      <section class="panel"><p class="eyebrow">EXPENSE REGISTER</p><h2>Recent expenses</h2>
        <p class="empty" *ngIf="!expenses().length">No expense records found in your permitted branch scope.</p>
        <div class="row" *ngFor="let expense of expenses()">
          <div><strong>{{ expense.description }}</strong><small>{{ expense.name }} · {{ expense.expenseDate | date:'mediumDate' }} · {{ expense.type }}</small>
            <small *ngIf="expense.salesOrderId || expense.tripId">Order {{ expense.salesOrderId || '—' }} · Trip {{ expense.tripId || '—' }}</small></div>
          <div class="right"><strong>{{ expense.amount | currency:'INR':'symbol':'1.2-2' }}</strong><span class="chip">{{ expense.status }}</span>
            <button *ngIf="expense.status === 'Pending approval'" class="secondary" (click)="sync(expense)">Sync approval</button>
            <button *ngIf="expense.status === 'Approved'" class="primary" (click)="post(expense)">Post to books</button>
          </div>
        </div>
      </section>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1450px;margin:auto;color:var(--ink,#292723)}.page-head{display:flex;justify-content:space-between;gap:18px;align-items:flex-start}
    .eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}h1{font-size:30px;margin:4px 0}.muted,small{color:#77736c}small{display:block;margin-top:4px}
    .panel{background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:18px;margin:14px 0}.notice{background:#f0e8de;border-radius:6px;padding:12px;line-height:1.5;font-size:13px}
    .error{color:#a3342a}.success{color:#24734f}.form{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;margin-bottom:16px}
    label{font-size:12px;color:#57524a;display:flex;flex-direction:column;gap:5px}input,select{font:inherit;padding:9px;border:1px solid #d7d1c8;border-radius:6px;min-width:0}.wide{grid-column:span 2}
    .check{flex-direction:row;align-items:center}.check input{min-width:auto}.primary,.secondary{border:0;border-radius:6px;padding:9px 13px;cursor:pointer}
    .primary{background:#a65d3b;color:#fff}.secondary{background:#eee9e1;color:#39352f}.row{display:flex;justify-content:space-between;align-items:center;gap:15px;padding:12px 0;border-bottom:1px solid #eee9e1}
    .right{display:flex;gap:9px;align-items:center;flex-wrap:wrap;justify-content:flex-end}.chip{background:#eee9e1;border-radius:20px;padding:4px 9px;font-size:11px}.empty{color:#77736c;padding:12px}
    @media(max-width:760px){.workspace{padding:18px 14px}.form{grid-template-columns:repeat(2,minmax(0,1fr))}.row{align-items:flex-start}.right{flex-direction:column}}
  `],
})
export class M14Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly categories = signal<any[]>([]);
  readonly expenses = signal<any[]>([]);
  readonly error = signal('');
  readonly notice = signal('');
  readonly saving = signal(false);
  draft: any = this.newDraft();
  categoryDraft: any = { name: '', type: 'Direct', approvalThreshold: '', expenseLedgerCode: '' };

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.get<any[]>('/m14/categories').subscribe({
      next: (data) => this.categories.set(data),
      error: () => this.error.set('Expense categories are unavailable. Configure M14 categories and permissions.'),
    });
    this.api.get<any[]>('/m14/expenses').subscribe({
      next: (data) => this.expenses.set(data),
      error: () => this.error.set('Expense register is unavailable. Check M14 activation and branch scope.'),
    });
  }

  createCategory(): void {
    const body = {
      ...this.categoryDraft,
      approvalThreshold: this.categoryDraft.approvalThreshold === '' ? null : Number(this.categoryDraft.approvalThreshold),
    };
    this.api.post<any, any>('/m14/categories', body).subscribe({
      next: () => {
        this.notice.set('Expense category added.');
        this.categoryDraft = { name: '', type: 'Direct', approvalThreshold: '', expenseLedgerCode: '' };
        this.load();
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Category creation requires M14 category-management permission and a configured ledger.'),
    });
  }

  create(): void {
    if (!this.draft.categoryId || !this.draft.branchId || !this.draft.amount || !this.draft.description) {
      this.error.set('Category, branch, amount and description are required.'); return;
    }
    this.saving.set(true); this.error.set('');
    const body = {
      ...this.draft, amount: Number(this.draft.amount), gstAmount: Number(this.draft.gstAmount || 0),
      branchId: this.draft.branchId, categoryId: this.draft.categoryId,
      supplierBillNumber: this.draft.supplierBillNumber || null,
      supplierGstin: this.draft.supplierGstin || null,
      siteId: this.draft.siteId || null, salesOrderId: this.draft.salesOrderId || null,
      tripId: this.draft.tripId || null, vendorPartyId: null, reverseCharge: false,
      idempotencyKey: crypto.randomUUID(),
    };
    this.api.post<any, any>('/m14/expenses', body).subscribe({
      next: (result) => {
        this.notice.set(`Expense saved as ${result.status}.`);
        this.draft = this.newDraft(); this.saving.set(false); this.load();
      },
      error: (error) => { this.error.set(error?.error?.detail ?? 'Expense could not be saved.'); this.saving.set(false); },
    });
  }

  sync(expense: any): void {
    this.api.post<any, any>(`/m14/expenses/${expense.expenseId}/sync-approval`, {}).subscribe({
      next: () => this.load(), error: () => this.error.set('Approval status could not be synchronized.'),
    });
  }

  post(expense: any): void {
    this.api.post<any, any>(`/m14/expenses/${expense.expenseId}/post`, {}).subscribe({
      next: (result) => { this.notice.set(`Posted as voucher ${result.voucherNumber}.`); this.load(); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Expense posting failed; verify ledger configuration and M12 period status.'),
    });
  }

  private newDraft(): any {
    return {
      branchId: '', categoryId: '', expenseDate: new Date().toISOString().slice(0, 10),
      amount: '', description: '', paidFromLedgerCode: '', employeePaid: false,
      supplierGstin: '', supplierBillNumber: '', gstAmount: 0, itcEligible: false,
      attachmentReference: '', siteId: '', salesOrderId: '', tripId: '',
    };
  }
}

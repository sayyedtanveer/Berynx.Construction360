import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M10Page } from './m10.page';

describe('M10Page', () => {
  it('loads draft invoices and reports an empty state', async () => {
    const api = { get: vi.fn(() => of({ items: [], total: 0 })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M10Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M10Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.get).toHaveBeenCalledWith('/m10/invoices?status=Draft&page=1&pageSize=50');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No invoices match');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Posting rechecks verified M08/M18 evidence');
  });

  it('refreshes the persisted M08/M09 eligible challan list on demand', async () => {
    const api = { get: vi.fn(() => of({ items: [], total: 0 })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M10Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M10Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.openDraft();
    await fixture.whenStable();
    page.refreshEligible();
    await fixture.whenStable();

    expect(api.get).toHaveBeenCalledTimes(3);
    expect(api.get).toHaveBeenNthCalledWith(2, '/m10/eligible-challans');
    expect(api.get).toHaveBeenNthCalledWith(3, '/m10/eligible-challans');
    expect(page.eligibilityLoading()).toBe(false);
  });

  it('requests the server-derived Paid filter instead of presenting an unsupported approval tab', async () => {
    const api = { get: vi.fn(() => of({ items: [], total: 0 })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M10Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M10Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;

    expect(page.statuses).not.toContain('Pending approval');
    page.setFilter('Paid');

    expect(api.get).toHaveBeenLastCalledWith('/m10/invoices?status=Paid&page=1&pageSize=50');
  });

  it('does not claim posting readiness when only an invoice draft has been saved', async () => {
    const api = {
      get: vi.fn((path: string) => of(path.includes('invoice-1')
        ? { invoice: { invoiceId: 'invoice-1', number: '', status: 'Draft' }, lines: [], challans: [], events: [] }
        : { items: [], total: 0 })),
      post: vi.fn(() => of({ invoiceId: 'invoice-1' })),
    };
    await TestBed.configureTestingModule({
      imports: [M10Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M10Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.eligible.set([{ challanId: 'challan-1', billingMode: 'PerOrder' }]);
    page.selectedIds.add('challan-1');
    page.createDraft();
    await fixture.whenStable();

    expect(page.notice()).toContain('Invoice draft saved');
    expect(page.notice()).not.toContain('Posting available: yes');
  });

  it('posts the selected challans to the invoice-draft contract and refreshes persisted state', async () => {
    await TestBed.configureTestingModule({
      imports: [M10Page],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(M10Page);
    fixture.detectChanges();

    http.expectOne('/api/v1/m10/invoices?status=Draft&page=1&pageSize=50')
      .flush({ items: [], total: 0, page: 1, pageSize: 50 });

    const page = fixture.componentInstance;
    page.openDraft();
    http.expectOne('/api/v1/m10/eligible-challans')
      .flush({ items: [{ challanId: 'challan-1', billingMode: 'PerOrder' }], pendingVerification: 0 });
    page.selectedIds.add('challan-1');
    page.invoiceDate = '2026-05-14';
    page.notes = 'May delivery';
    page.billingModeOverride = 'PerChallan';
    page.billingModeOverrideReason = 'Customer requested separate invoices';
    page.createDraft();

    const create = http.expectOne('/api/v1/m10/invoices/drafts');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({
      challanIds: ['challan-1'],
      invoiceDate: '2026-05-14',
      notes: 'May delivery',
      idempotencyKey: page['idempotencyKey'],
      billingModeOverride: 'PerChallan',
      billingModeOverrideReason: 'Customer requested separate invoices',
    });
    create.flush({ invoiceId: 'invoice-1' });

    http.expectOne('/api/v1/m10/invoices?status=Draft&page=1&pageSize=50')
      .flush({ items: [{ invoiceId: 'invoice-1', status: 'Draft' }], total: 1, page: 1, pageSize: 50 });
    http.expectOne('/api/v1/m10/invoices/invoice-1')
      .flush({ invoice: { invoiceId: 'invoice-1', status: 'Draft' }, lines: [], challans: [], events: [] });

    expect(page.items()).toEqual([{ invoiceId: 'invoice-1', status: 'Draft' }]);
    expect(page.detail()?.invoice.invoiceId).toBe('invoice-1');
    expect(page.notice()).toContain('Invoice draft saved');
    http.verify();
  });

  it('cancels a posted invoice with the versioned reason contract and refreshes detail and list', async () => {
    await TestBed.configureTestingModule({
      imports: [M10Page],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(M10Page);
    fixture.detectChanges();
    http.expectOne('/api/v1/m10/invoices?status=Draft&page=1&pageSize=50')
      .flush({ items: [], total: 0, page: 1, pageSize: 50 });

    const page = fixture.componentInstance;
    page.openDetail('invoice-1');
    http.expectOne('/api/v1/m10/invoices/invoice-1').flush({
      invoice: { invoiceId: 'invoice-1', status: 'Posted', version: 7 },
      lines: [], challans: [], events: [],
    });
    page.cancelReason = 'Duplicate billing';
    page.cancelPostedInvoice();

    const cancel = http.expectOne('/api/v1/m10/invoices/invoice-1/cancel');
    expect(cancel.request.method).toBe('POST');
    expect(cancel.request.body).toEqual({ version: 7, reason: 'Duplicate billing' });
    cancel.flush({
      invoiceId: 'invoice-1', status: 'Cancelled',
      reversalVoucherId: 'voucher-reversal', reversalVoucherNumber: 'SV-2026-000002',
      releasedChallanCount: 2,
    });

    http.expectOne('/api/v1/m10/invoices/invoice-1').flush({
      invoice: { invoiceId: 'invoice-1', status: 'Cancelled', version: 8 },
      lines: [], challans: [], events: [],
    });
    http.expectOne('/api/v1/m10/invoices?status=Draft&page=1&pageSize=50')
      .flush({ items: [], total: 0, page: 1, pageSize: 50 });

    expect(page.notice()).toContain('SV-2026-000002');
    expect(page.detail()?.invoice.status).toBe('Cancelled');
    expect(page.cancelReason).toBe('');
    http.verify();
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M11Page } from './m11.page';

describe('M11Page', () => {
  it('shows receipt drafts and the M12 posting boundary', async () => {
    const rows = [{ receiptId: 'r-1', version: 3, receiptDate: '2026-05-10', status: 'Draft', customerPartyId: 'c-1', paymentMethod: 'Bank transfer', amount: 40, allocatedAmount: 30, advanceAmount: 10 }];
    const api = { get: vi.fn(() => of(rows)), post: vi.fn(() => of({ number: 'RC-1' })) };
    await TestBed.configureTestingModule({
      imports: [M11Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M11Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.get).toHaveBeenCalledWith('/m11/receipts');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Cash, bank-transfer and UPI drafts can post');
    const postButton = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find(button => button.textContent?.trim() === 'Post');
    expect(postButton).toBeTruthy();
    postButton?.click();
    await fixture.whenStable();
    expect(api.post).toHaveBeenCalledWith('/m11/receipts/r-1/post', { version: 3, reason: 'Posted after receipt evidence review' });
  });

  it('loads tenant-scoped customer and branch choices and requests invoices for the selected branch', async () => {
    const api = {
      get: vi.fn((path: string) => of(path === '/m11/receipts'
        ? []
        : path === '/m11/receipt-options'
          ? {
              customers: [{ partyId: 'customer-1', legalName: 'North Build', tradeName: 'North Build', gstin: '27ABCDE1234F1Z5' }],
              branches: [{ id: 'branch-1', name: 'Pune', gstin: '27ABCDE1234F1Z5' }],
            }
          : { items: [] })),
      post: vi.fn(() => of({})),
    };
    await TestBed.configureTestingModule({
      imports: [M11Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M11Page);
    fixture.detectChanges();
    await fixture.whenStable();

    const page = fixture.componentInstance;
    page.openDraft();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledWith('/m11/receipt-options');
    expect(page.customers()[0].partyId).toBe('customer-1');
    expect(page.branches()[0].id).toBe('branch-1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('North Build');
    page.form.customerPartyId = 'customer-1';
    page.loadOpenInvoices();
    expect(api.get).toHaveBeenLastCalledWith('/m11/customers/customer-1/open-invoices?branchId=branch-1');
  });

  it('submits a receipt draft using the M11 request contract and reloads the receipt list', async () => {
    await TestBed.configureTestingModule({
      imports: [M11Page],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(M11Page);
    fixture.detectChanges();
    http.expectOne('/api/v1/m11/receipts').flush([]);

    const page = fixture.componentInstance;
    page.openDraft();
    http.expectOne('/api/v1/m11/receipt-options').flush({
      customers: [{ partyId: 'customer-1', legalName: 'North Build', tradeName: 'North Build' }],
      branches: [{ id: 'branch-1', name: 'Pune' }],
    });
    page.form = {
      customerPartyId: 'customer-1',
      branchId: 'branch-1',
      receiptDate: '2026-05-14',
      paymentMethod: 'Bank transfer',
      referenceNumber: 'UTR-123',
      amount: 100,
      tdsAmount: 5,
      bankCharges: 1,
      notes: 'May receipt',
    };
    page.allocations = { 'invoice-1': 60 };
    page.saveDraft();

    const create = http.expectOne('/api/v1/m11/receipts/drafts');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({
      customerPartyId: 'customer-1',
      branchId: 'branch-1',
      receiptDate: '2026-05-14',
      paymentMethod: 'Bank transfer',
      referenceNumber: 'UTR-123',
      amount: 100,
      tdsAmount: 5,
      bankCharges: 1,
      notes: 'May receipt',
      allocations: [{ invoiceId: 'invoice-1', amount: 60 }],
      idempotencyKey: page.idempotencyKey,
    });
    create.flush({ receiptId: 'receipt-1', advanceAmount: 40 });
    http.expectOne('/api/v1/m11/receipts').flush([
      { receiptId: 'receipt-1', status: 'Draft', amount: 100 },
    ]);

    expect(page.items()).toEqual([{ receiptId: 'receipt-1', status: 'Draft', amount: 100 }]);
    expect(page.notice()).toContain('Advance 40');
    http.verify();
  });
});

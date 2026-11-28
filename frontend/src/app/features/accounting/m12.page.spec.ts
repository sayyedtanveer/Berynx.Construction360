import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M12Page } from './m12.page';

describe('M12Page', () => {
  it('loads the real trial-balance endpoint and empty state', async () => {
    const api = { get: vi.fn(() => of({ balances: [], items: [] })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M12Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M12Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.get).toHaveBeenCalledWith('/m12/trial-balance');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No posted voucher balances');
  });

  it('uses the API-provided trial-balance totals instead of floating-point re-aggregation', async () => {
    const trialBalance = {
      balances: [
        { ledgerCode: '1000', debit: 0.1, credit: 0 },
        { ledgerCode: '2000', debit: 0.2, credit: 0.3 },
      ],
      totalDebit: 0.3,
      totalCredit: 0.3,
    };
    const api = {
      get: vi.fn(() => of(trialBalance)),
      post: vi.fn(() => of({})),
    };
    await TestBed.configureTestingModule({
      imports: [M12Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M12Page);
    fixture.detectChanges();
    await fixture.whenStable();

    const page = fixture.componentInstance;
    expect(page.totalDebit()).toBe(0.3);
    expect(page.totalCredit()).toBe(0.3);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('0.30');
  });
  it('saves a tenant ledger account using the current chart version', async () => {
    const api = { get: vi.fn(() => of({ balances: [], items: [], version: 7, branches: [], accounts: [], codes: [] })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M12Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M12Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.loadAccounts();
    await fixture.whenStable();
    page.ledgerForm = { code: 'project-cost', name: 'Project costs', group: 'Expenses' };
    page.createAccount();
    expect(api.post).toHaveBeenCalledWith('/m12/accounts', {
      code: 'project-cost', name: 'Project costs', group: 'Expenses', version: 7,
    });
  });

  it('uses active chart-of-accounts entries for manual voucher lines', async () => {
    const api = {
      get: vi.fn((path: string) => of(path === '/m12/accounts'
        ? { version: 2, items: [
            { code: 'cash', name: 'Cash', isActive: true },
            { code: 'old-ledger', name: 'Inactive ledger', isActive: false },
          ] }
        : { balances: [], totalDebit: 0, totalCredit: 0 })),
      post: vi.fn(() => of({})),
    };
    await TestBed.configureTestingModule({
      imports: [M12Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M12Page);
    fixture.detectChanges();
    const page = fixture.componentInstance;
    page.openJournal();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledWith('/m12/accounts');
    expect(page.activeAccounts().map(account => account.code)).toEqual(['cash']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Cash');
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Inactive ledger');
  });

  it('posts a manual voucher with branch dimensions and refreshes accounting views', async () => {
    await TestBed.configureTestingModule({
      imports: [M12Page],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(M12Page);
    fixture.detectChanges();
    http.expectOne('/api/v1/m12/trial-balance').flush({
      balances: [], totalDebit: 0, totalCredit: 0,
    });

    const page = fixture.componentInstance;
    page.openJournal();
    http.expectOne('/api/v1/m12/accounts').flush({
      version: 5, items: [{ code: 'cash', name: 'Cash', isActive: true }],
    });
    page.form = {
      voucherType: 'Journal',
      branchId: 'branch-1',
      voucherDate: '2026-05-14',
      narration: 'Opening adjustment',
    };
    page.lines = [
      { ledgerCode: 'cash', partyId: '', debit: 10, credit: 0, narration: 'Debit' },
      { ledgerCode: 'revenue', partyId: '', debit: 0, credit: 10, narration: 'Credit' },
    ];
    page.postJournal();

    const post = http.expectOne('/api/v1/m12/vouchers/manual');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({
      voucherType: 'Journal',
      branchId: 'branch-1',
      voucherDate: '2026-05-14',
      narration: 'Opening adjustment',
      idempotencyKey: expect.any(String),
      lines: [
        {
          ledgerCode: 'cash', partyId: null, debit: 10, credit: 0, narration: 'Debit',
          branchId: 'branch-1', siteId: null, salesOrderId: null, tripId: null,
        },
        {
          ledgerCode: 'revenue', partyId: null, debit: 0, credit: 10, narration: 'Credit',
          branchId: 'branch-1', siteId: null, salesOrderId: null, tripId: null,
        },
      ],
    });
    post.flush({ number: 'JV-001' });
    http.expectOne('/api/v1/m12/trial-balance').flush({
      balances: [], totalDebit: 10, totalCredit: 10,
    });
    http.expectOne('/api/v1/m12/vouchers').flush({ items: [{ number: 'JV-001' }] });

    expect(page.notice()).toBe('Voucher JV-001 posted.');
    expect(page.totalDebit()).toBe(10);
    expect(page.vouchers()).toEqual([{ number: 'JV-001' }]);
    http.verify();
  });
});

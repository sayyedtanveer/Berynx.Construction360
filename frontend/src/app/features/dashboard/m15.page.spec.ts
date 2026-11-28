import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M15Page } from './m15.page';

describe('M15Page', () => {
  it('loads transaction-backed dashboard data with a date range', async () => {
    const api = { get: vi.fn(() => of({
      sales: { orders: 0, orderValue: 0 }, invoices: { postedCount: 0, postedValue: 0, outstandingEstimate: 0 },
      operations: { trips: 0, deliveredQuantity: 0, plannedQuantity: 0, unbilledChallans: 0, byStatus: {} },
      expenses: { costVisible: false }, attention: { pendingApprovals: 0, pendingInvoicePost: 0, receiptsToAllocate: 0 },
      unavailable: [],
    })) };
    await TestBed.configureTestingModule({
      imports: [M15Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M15Page);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(api.get).toHaveBeenCalledWith(expect.stringMatching('/m15/home\\?from=\\d{4}-\\d{2}-\\d{2}&to=\\d{4}-\\d{2}-\\d{2}'));
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Unbilled challans');
  });
});

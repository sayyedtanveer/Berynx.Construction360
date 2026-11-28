import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M06Page } from './m06.page';

describe('M06Page', () => {
  it('loads persisted orders and labels unavailable downstream modules without fabricating data', async () => {
    const api = {
      get: vi.fn((path: string) => {
        if (path.startsWith('/m06/orders?')) {
          return of({ rows: [{
            salesOrderId: 'order-1', number: 'SO-TEST-1', status: 'Confirmed',
            customerNameSnapshot: 'Test customer', orderDate: '2026-04-10', total: 1250,
          }], total: 1 });
        }
        if (path === '/m06/orders/order-1') {
          return of({
            salesOrderId: 'order-1', number: 'SO-TEST-1', status: 'Confirmed',
            customerNameSnapshot: 'Test customer', orderDate: '2026-04-10',
            fulfilment: {
              orderedQuantity: 5, scheduledQuantity: 0, deliveredQuantity: null,
              invoicedQuantity: null, paidAmount: null,
              downstreamState: 'Pending modules M08, M10 and M11',
            },
            lines: [], schedules: [], amendments: [], activities: [], documents: [],
            purchaseOrders: { state: 'unavailable' }, dispatches: { state: 'unavailable' },
          });
        }
        if (path.startsWith('/m05/quotations')) return of({ rows: [] });
        return of([]);
      }),
      post: vi.fn(() => of({})),
      getText: vi.fn(() => of('')),
    };
    await TestBed.configureTestingModule({
      imports: [M06Page],
      providers: [{ provide: ApiClient, useValue: api }],
    }).compileComponents();

    const fixture = TestBed.createComponent(M06Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledWith(expect.stringContaining('/m06/orders?'));
    expect(api.get).toHaveBeenCalledWith('/m03/parties?role=Customer');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('SO-TEST-1');

    fixture.componentInstance.openDetail({ salesOrderId: 'order-1' });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Delivered · M08 pending');
    expect(text).toContain('No downstream PO, dispatch, challan, invoice or receipt is persisted');
  });
});

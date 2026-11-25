import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M04Page } from './m04.page';

describe('M04Page', () => {
  it('loads enquiries and reusable M03 references', async () => {
    const api = {
      get: vi.fn((path: string) => of(path.startsWith('/m04/enquiries')
        ? { rows: [{ enquiryId: 'enq-1', number: 'ENQ-2026-000001', customerPartyId: 'customer-1', customerName: 'North Build', status: 'New', source: 'Phone' }], total: 1 }
        : path.includes('parties') ? [{ partyId: 'customer-1', legalName: 'North Build' }]
          : path.includes('products') ? [{ productId: 'product-1', name: 'Cement', baseUnitCode: 'BAG' }]
            : [])),
      post: vi.fn(() => of({ enquiryId: 'enq-2' })),
    };
    await TestBed.configureTestingModule({
      imports: [M04Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();

    const fixture = TestBed.createComponent(M04Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledWith(expect.stringContaining('/m04/enquiries'));
    expect(api.get).toHaveBeenCalledWith('/m03/parties?role=Customer');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('ENQ-2026-000001');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('North Build');
  });

  it('posts a captured enquiry to the M04 API with an idempotency key', async () => {
    const api = {
      get: vi.fn(() => of({ rows: [], total: 0 })),
      post: vi.fn(() => of({ enquiryId: 'enq-3' })),
    };
    await TestBed.configureTestingModule({
      imports: [M04Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M04Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.draft = {
      source: 'Phone', customerPartyId: 'customer-1', contactId: '', siteId: '', branchId: '',
      productId: 'product-1', quantity: 2, unitCode: 'BAG', priority: 'Normal',
      expectedValue: '', requiredBy: '', nextActionAt: '2026-12-01T10:00',
      notes: 'Cement requirement',
    };

    page.create();

    expect(api.post).toHaveBeenCalledWith('/m04/enquiries', expect.objectContaining({
      customerPartyId: 'customer-1',
      lines: [expect.objectContaining({ productId: 'product-1', quantity: 2, unitCode: 'BAG' })],
      idempotencyKey: expect.any(String),
    }));
  });

  it('reopens a lost enquiry only with an open stage and next-action date', async () => {
    const api = {
      get: vi.fn(() => of({ rows: [], total: 0 })),
      post: vi.fn(() => of({ enquiryId: 'enq-lost', status: 'Follow-up' })),
    };
    await TestBed.configureTestingModule({
      imports: [M04Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M04Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.reopenStatus = 'Follow-up';
    page.reopenNextActionAt = '2026-12-05T10:00';

    page.reopen({ enquiryId: 'enq-lost', version: 4 });

    expect(api.post).toHaveBeenCalledWith('/m04/enquiries/enq-lost/reopen', expect.objectContaining({
      status: 'Follow-up', lostReason: null, version: 4,
      nextActionAt: new Date('2026-12-05T10:00').toISOString(),
    }));
  });
});

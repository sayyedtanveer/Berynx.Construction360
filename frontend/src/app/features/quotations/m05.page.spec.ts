import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M05Page } from './m05.page';

describe('M05Page', () => {
  it('loads quotations and reuses M03 customer/product masters', async () => {
    const api = {
      get: vi.fn((path: string) => of(path.startsWith('/m05/quotations')
        ? { rows: [{ quotationId: 'quote-1', number: 'QT-2026-000001', status: 'Approved' }] }
        : [])),
      post: vi.fn(() => of({})),
      getText: vi.fn(() => of('')),
    };
    await TestBed.configureTestingModule({
      imports: [M05Page],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap({})) } },
        { provide: ApiClient, useValue: api },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(M05Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledWith(expect.stringContaining('/m05/quotations'));
    expect(api.get).toHaveBeenCalledWith('/m03/parties?role=Customer');
    expect(api.get).toHaveBeenCalledWith('/m03/products');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('QT-2026-000001');
  });

  it('creates a quotation using server-resolved price and tax inputs', async () => {
    const api = {
      get: vi.fn(() => of({ rows: [] })),
      post: vi.fn(() => of({ quotationId: 'quote-2' })),
      getText: vi.fn(() => of('')),
    };
    await TestBed.configureTestingModule({
      imports: [M05Page],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap({})) } },
        { provide: ApiClient, useValue: api },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(M05Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.draft = {
      customerPartyId: 'customer-1', siteId: '', contactId: '', branchId: '',
      productId: 'product-1', quantity: 5, unitCode: 'BAG', discountPercent: 0,
      priceBasis: 'ExPlant', validUntil: '2026-12-10', paymentTermId: '',
      placeOfSupplyStateCode: '27', deliveryTerms: 'Deliver to site',
      termsAndConditions: 'Valid until date shown', internalNotes: '',
      charges: [],
    };

    page.save();

    expect(api.post).toHaveBeenCalledWith('/m05/quotations', expect.objectContaining({
      customerPartyId: 'customer-1',
      lines: [expect.objectContaining({ productId: 'product-1', rate: null, quantity: 5 })],
      idempotencyKey: expect.any(String),
    }));
  });

  it('submits multiple lines and configured charges', async () => {
    const api = {
      get: vi.fn(() => of({ rows: [] })),
      post: vi.fn(() => of({ quotationId: 'quote-multiple' })),
      getText: vi.fn(() => of('')),
    };
    await TestBed.configureTestingModule({
      imports: [M05Page],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap({})) } },
        { provide: ApiClient, useValue: api },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(M05Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.draft = {
      customerPartyId: 'customer-1', siteId: '', contactId: '', branchId: '',
      lines: [
        { productId: 'product-1', quantity: 5, unitCode: 'BAG', discountPercent: 0 },
        { productId: 'product-2', quantity: 2, unitCode: 'MT', discountPercent: 1 },
      ],
      charges: [{ chargeTypeId: 'charge-1', description: 'Transport', amount: 250 }],
      priceBasis: 'Delivered', validUntil: '2026-12-10', paymentTermId: '',
      placeOfSupplyStateCode: '27', deliveryTerms: 'Deliver to site',
      termsAndConditions: 'Valid until date shown', internalNotes: '',
    };

    page.save();

    expect(api.post).toHaveBeenCalledWith('/m05/quotations', expect.objectContaining({
      lines: [
        expect.objectContaining({ productId: 'product-1', quantity: 5, rate: null }),
        expect.objectContaining({ productId: 'product-2', quantity: 2, rate: null }),
      ],
      charges: [expect.objectContaining({ chargeTypeId: 'charge-1', amount: 250 })],
    }));
  });
});

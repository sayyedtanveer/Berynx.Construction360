import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';
import { ApiClient } from '../../core/api/api-client.service';
import { M03Page } from './m03.page';

describe('M03Page', () => {
  it('renders customer master fields and loaded list data', async () => {
    const api = {
      get: vi.fn(() => of([{
        partyId: 'party-1',
        legalName: 'Example Customer',
        partyType: 'Business',
        roles: ['Customer'],
        creditLimit: 25000,
        creditDays: 30,
        billingMode: 'Monthly',
        creditPolicy: 'Warn',
        creditStatus: 'Active',
        version: 1,
      }])),
      post: vi.fn(() => of({})),
      put: vi.fn(() => of({})),
      getBlob: vi.fn(() => of(new Blob())),
    };

    await TestBed.configureTestingModule({
      imports: [M03Page],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ screen: 'customers' })) } },
        { provide: ApiClient, useValue: api },
        { provide: API_BASE_URL, useValue: '/api/v1' },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelector('h1')?.textContent).toContain('Customers');
    expect(page.textContent).toContain('Credit limit');
    expect(page.textContent).toContain('Invoice billing mode');
    expect(page.textContent).toContain('Example Customer');
    expect(page.textContent).toContain('Edit');
  });

  it('retains the row version when editing an existing customer', async () => {
    const api = {
      get: vi.fn(() => of([{
        partyId: 'party-2',
        legalName: 'Versioned Customer',
        partyType: 'Business',
        roles: ['Customer'],
        creditLimit: 100,
        creditDays: 10,
        billingMode: 'PerOrder',
        creditPolicy: 'Warn',
        creditStatus: 'Active',
        version: 7,
      }])),
      post: vi.fn(() => of({})),
      put: vi.fn(() => of({})),
      getBlob: vi.fn(() => of(new Blob())),
    };

    await TestBed.configureTestingModule({
      imports: [M03Page],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ screen: 'customers' })) } },
        { provide: ApiClient, useValue: api },
        { provide: API_BASE_URL, useValue: '/api/v1' },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    const edit = [...page.querySelectorAll('button')].find(button => button.textContent?.includes('Edit'));
    edit?.click();
    fixture.detectChanges();
    const form = page.querySelector('form');
    form?.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(api.put).toHaveBeenCalledWith('/m03/parties/party-2', expect.objectContaining({
      version: 7,
      legalName: 'Versioned Customer',
      billingMode: 'PerOrder',
    }));
  });

  it('keeps parent form data while selecting a newly created contextual customer', async () => {
    const api = {
      get: vi.fn(() => of([])),
      post: vi.fn(() => of({ partyId: 'new-customer' })),
      put: vi.fn(() => of({})),
      getBlob: vi.fn(() => of(new Blob())),
    };
    await TestBed.configureTestingModule({
      imports: [M03Page],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ screen: 'sites' })) } },
        { provide: ApiClient, useValue: api },
        { provide: API_BASE_URL, useValue: '/api/v1' },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.form = { name: 'Main site', city: 'Pune', address: 'Address' };
    const field = page.config.fields.find(candidate => candidate.key === 'customerPartyId')!;
    page.openContextCreate(field);
    page.onContextCreated({ partyId: 'new-customer' });

    expect(page.form).toEqual({
      name: 'Main site', city: 'Pune', address: 'Address', customerPartyId: 'new-customer',
    });
  });

  it('never posts a masked account number when editing supplier bank metadata', async () => {
    const api = {
      get: vi.fn((path: string) => {
        if (path === '/m03/parties?role=Supplier') return of([{ partyId: 'supplier-1', legalName: 'Supplier' }]);
        if (path === '/m03/capabilities') return of({
          canCreate: true, canEdit: true, canDeactivate: true, canViewAudit: false,
        });
        if (path.includes('/bank-accounts')) return of([{
          bankAccountId: 'bank-1', accountHolder: 'Supplier', maskedAccountNumber: '••••1234',
          ifsc: 'ABCD0123456', bankName: 'Bank', isPrimary: true, version: 4,
        }]);
        return of([]);
      }),
      post: vi.fn(() => of({})),
      put: vi.fn(() => of({ bankAccountId: 'bank-1' })),
      getBlob: vi.fn(() => of(new Blob())),
    };
    await TestBed.configureTestingModule({
      imports: [M03Page],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ screen: 'supplier-bank-accounts' })) } },
        { provide: ApiClient, useValue: api },
        { provide: API_BASE_URL, useValue: '/api/v1' },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.selectedSupplierId = 'supplier-1';
    page.editingBankAccount = {
      bankAccountId: 'bank-1', accountHolder: 'Supplier', maskedAccountNumber: '••••1234',
      ifsc: 'ABCD0123456', bankName: 'Bank', isPrimary: true, version: 4,
    };
    page.bankForm = {
      accountHolder: 'Supplier Ltd', accountNumber: '', ifsc: 'ABCD0123456',
      bankName: 'Updated Bank', isPrimary: true,
    };
    page.saveSupplierBankAccount();
    await fixture.whenStable();

    expect(api.put).toHaveBeenCalledWith('/m03/party-bank-accounts/bank-1', expect.objectContaining({
      accountNumber: null,
      version: 4,
    }));
    expect(JSON.stringify(api.put.mock.calls)).not.toContain('••••1234');
  });

  it('renders explicit unavailable states instead of fabricated Customer 360 metrics', async () => {
    const customerView = {
      party: { partyId: 'customer-1', legalName: 'Actual Customer', gstin: '27AAPFU0939F1ZV', creditStatus: 'Active' },
      sites: [], contacts: [], pricingAgreements: [], activity: [], activityAvailable: true,
      creditDecision: { status: 'Warning', reasons: ['No transaction source.'], canRequestOverride: false },
      creditFacts: {
        available: false, outstanding: null, overdue: null,
        unavailableReason: 'Not available until accounting is activated.',
      },
      metrics: {
        available: false, invoicedAmount: null, collections: null, lastOrderAt: null,
        ytdSales: null, ytdProfit: null, unavailableReason: 'Not available until transactions are activated.',
      },
      linkedDocuments: { available: false, message: 'Not available until document modules are activated.' },
      unavailableIntegrations: ['Orders', 'Invoices'],
    };
    const api = {
      get: vi.fn((path: string) => {
        if (path === '/m03/parties?role=Customer') {
          return of([{ partyId: 'customer-1', legalName: 'Actual Customer' }]);
        }
        if (path.endsWith('/customer-1/360')) return of(customerView);
        return of([]);
      }),
      post: vi.fn(() => of({})),
      put: vi.fn(() => of({})),
      getBlob: vi.fn(() => of(new Blob())),
    };
    await TestBed.configureTestingModule({
      imports: [M03Page],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ screen: 'customer-360' })) } },
        { provide: ApiClient, useValue: api },
        { provide: API_BASE_URL, useValue: '/api/v1' },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.form['partyId'] = 'customer-1';
    page.loadCustomer360();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Actual Customer');
    expect(fixture.nativeElement.textContent).toContain('Not available until accounting is activated.');
    expect(fixture.nativeElement.textContent).toContain('Not available until transactions are activated.');
    expect(fixture.nativeElement.textContent).not.toContain('YTD sales 0');
  });

  it('sends GSTIN and place-of-supply inputs to the tax resolution API', async () => {
    const api = {
      get: vi.fn((path: string) => path === '/m03/products'
        ? of([{ productId: 'product-1', code: 'RMC-1', name: 'M30', hsnCode: '6810' }])
        : of([])),
      post: vi.fn(() => of({
        productId: 'product-1', ratePercent: 18, source: 'Hsn',
        sellerGstin: '27AAPFU0939F1ZV', buyerGstin: '29AAPFU0939F1ZR',
        placeOfSupplyStateCode: '29',
      })),
      put: vi.fn(() => of({})),
      getBlob: vi.fn(() => of(new Blob())),
    };
    await TestBed.configureTestingModule({
      imports: [M03Page],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ screen: 'tax' })) } },
        { provide: ApiClient, useValue: api },
        { provide: API_BASE_URL, useValue: '/api/v1' },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.taxLookup = {
      productId: 'product-1', customerCategory: '', sellerGstin: '27AAPFU0939F1ZV',
      buyerGstin: '29AAPFU0939F1ZR', placeOfSupplyStateCode: '29', asOf: '2026-11-24',
    };
    page.resolveTaxRate();
    await fixture.whenStable();

    expect(api.post).toHaveBeenCalledWith('/m03/tax-rates/resolve', expect.objectContaining({
      productId: 'product-1',
      sellerGstin: '27AAPFU0939F1ZV',
      buyerGstin: '29AAPFU0939F1ZR',
      placeOfSupplyStateCode: '29',
    }));
    expect(page.taxResolution?.['ratePercent']).toBe(18);
  });
});

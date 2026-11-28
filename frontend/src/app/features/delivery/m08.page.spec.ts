import { TestBed } from '@angular/core/testing';
import { HttpEventType, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M08Page } from './m08.page';

describe('M08Page', () => {
  it('loads dispatch board and presents unscheduled M06 rows', async () => {
    const api = {
      get: vi.fn(() => of({
        unscheduled: [{ scheduleId: 'schedule-1', number: 'SO-1', customerNameSnapshot: 'Customer A', quantity: 8 }],
        pours: [],
        totals: { scheduled: 8, delivered: 0, pending: 8 },
      })),
      post: vi.fn(() => of({})),
    };
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M08Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledWith(expect.stringContaining('/m08/dispatch-board?date='));
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('SO-1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Schedule pour');
  });

  it('states receiver OTP is not used and files require a clean malware scan', async () => {
    const api = { get: vi.fn(() => of({ unscheduled: [], pours: [], totals: {} })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M08Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Receiver OTP is not used');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('configured malware scanner reports a clean result');
  });

  it('records a delivery without evidence but keeps it non-billable', async () => {
    const api = { get: vi.fn(() => of({ unscheduled: [], pours: [], totals: {} })), post: vi.fn(() => of({ billingEligibility: 'Unverified; not invoice-eligible' })) };
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M08Page);
    const page = fixture.componentInstance;
    page.actionDraft = {
      action: 'Completed', trip: { tripId: 'trip-1', version: 1 }, receiverName: 'Receiver',
      signatureReference: '', photoReference: '',
    };

    page.saveAction();

    expect(api.post).toHaveBeenCalledWith('/m08/trips/trip-1/actions', expect.objectContaining({
      action: 'Completed', signatureReference: null, photoReference: null,
    }));
    expect(page.notice()).toContain('Unverified; not invoice-eligible');
  });
  it('rejects unsupported evidence files before sending them to the API', async () => {
    const api = { get: vi.fn(() => of({ unscheduled: [], pours: [], totals: {} })), post: vi.fn(() => of({})), upload: vi.fn() };
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M08Page);
    const page = fixture.componentInstance;
    page.actionDraft = { trip: { tripId: 'trip-1', version: 1 }, evidenceUploads: {} };
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', { value: [new File(['text'], 'unsafe.txt', { type: 'text/plain' })] });

    page.selectEvidenceFile({ target: input } as unknown as Event, 'Signature', 'completion');

    expect(page.error()).toContain('JPEG, PNG or WebP');
    expect(api.upload).not.toHaveBeenCalled();
  });

  it('uploads signature evidence and records only the server-returned clean asset', async () => {
    const file = new File([new Uint8Array([0xff, 0xd8, 0xff])], 'receiver.jpg', { type: 'image/jpeg' });
    const api = {
      get: vi.fn(() => of({ unscheduled: [], pours: [], totals: {} })),
      post: vi.fn(() => of({})),
      upload: vi.fn(() => of({ type: HttpEventType.Response, body: { documentId: 'asset-1', scanStatus: 'Clean', versionNumber: 1 } })),
    };
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M08Page);
    const page = fixture.componentInstance;
    const trip = { tripId: 'trip-1', version: 1 };
    page.actionDraft = { trip, evidenceUploads: {}, selectedSignatureFile: file };

    page.uploadEvidence('Signature', 'completion');

    expect(api.upload).toHaveBeenCalledWith(expect.stringContaining('/m08/trips/trip-1/evidence-assets?purpose=Signature'), file);
    expect(page.actionDraft.trip.signatureReference).toBe('asset-1');
    expect(page.actionDraft.trip.version).toBe(2);
    expect(page.actionDraft.evidenceUploads.Signature.scanStatus).toBe('Clean');
  });
  it('restores quarantined evidence scans when a delivery is reopened', async () => {
    const api = {
      get: vi.fn((path: string) => path.includes('/evidence-assets/pending')
        ? of({ items: [{ documentId: 'pending-1', purpose: 'Signature', scanStatus: 'Pending', storageStatus: 'Quarantined' }] })
        : of({ unscheduled: [], pours: [], totals: {} })),
      post: vi.fn(() => of({})),
    };
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M08Page);
    const page = fixture.componentInstance;
    page.openResubmission({ tripId: 'trip-1', version: 3, receiverName: 'Receiver' });

    expect(api.get).toHaveBeenCalledWith('/m08/trips/trip-1/evidence-assets/pending');
    expect(page.evidenceDraft()?.evidenceUploads.Signature).toMatchObject({
      documentId: 'pending-1', scanStatus: 'Pending', storageStatus: 'Quarantined',
    });
  });
  it('does not treat a superseded clean scan as current receiver evidence', async () => {
    const api = { get: vi.fn(() => of({ unscheduled: [], pours: [], totals: {} })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M08Page);
    const page = fixture.componentInstance;
    const draft = { evidenceUploads: { Signature: { scanStatus: 'Clean', isCurrent: false } } };

    expect(page.hasCleanEvidence(draft)).toBe(false);
    expect(page.evidenceStatus(draft, 'Signature')).toContain('newer upload superseded');
  });

  it('posts a completed-trip action with its version and quantities, then reloads dispatch state', async () => {
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(M08Page);
    fixture.detectChanges();
    http.expectOne('/api/v1/m08/evidence/pending').flush({ items: [] });
    http.expectOne(request => request.url.startsWith('/api/v1/m08/dispatch-board?date='))
      .flush({ unscheduled: [], pours: [], totals: {} });

    const page = fixture.componentInstance;
    page.actionDraft = {
      action: 'Completed',
      trip: { tripId: 'trip-1', version: 4 },
      loadedQuantity: 10,
      deliveredQuantity: 9,
      returnedQuantity: 1,
      batchSlipNumber: 'BATCH-9',
      receiverName: 'Site receiver',
      receiverMobile: '',
    };
    page.saveAction();

    const action = http.expectOne('/api/v1/m08/trips/trip-1/actions');
    expect(action.request.method).toBe('POST');
    expect(action.request.body).toEqual({
      actionId: expect.any(String),
      version: 4,
      action: 'Completed',
      deviceAt: expect.any(String),
      loadedQuantity: null,
      deliveredQuantity: 9,
      returnedQuantity: 1,
      batchSlipNumber: 'BATCH-9',
      receiverName: 'Site receiver',
      receiverMobile: null,
      signatureReference: null,
      photoReference: null,
      exceptionType: null,
      exceptionReason: null,
    });
    action.flush({ status: 'Completed', billingEligibility: 'Unverified; not invoice-eligible' });
    http.expectOne('/api/v1/m08/evidence/pending').flush({ items: [] });
    http.expectOne(request => request.url.startsWith('/api/v1/m08/dispatch-board?date='))
      .flush({ unscheduled: [], pours: [], totals: { delivered: 9 } });

    expect(page.notice()).toBe('Unverified; not invoice-eligible');
    expect(page.board()?.totals.delivered).toBe(9);
    http.verify();
  });

  it('uploads a raw evidence file to M18 and keeps clean evidence unverified until review', async () => {
    await TestBed.configureTestingModule({
      imports: [M08Page],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(M08Page);
    fixture.detectChanges();
    http.expectOne('/api/v1/m08/evidence/pending').flush({ items: [] });
    http.expectOne(request => request.url.startsWith('/api/v1/m08/dispatch-board?date='))
      .flush({ unscheduled: [], pours: [], totals: {} });

    const page = fixture.componentInstance;
    const file = new File([new Uint8Array([0xff, 0xd8, 0xff])], 'receiver.jpg', { type: 'image/jpeg' });
    page.actionDraft = {
      trip: { tripId: 'trip-1', version: 2 },
      evidenceUploads: {},
      selectedSignatureFile: file,
    };
    page.uploadEvidence('Signature', 'completion');

    const upload = http.expectOne('/api/v1/m08/trips/trip-1/evidence-assets?purpose=Signature&fileName=receiver.jpg');
    expect(upload.request.method).toBe('POST');
    expect(upload.request.body).toBe(file);
    expect(upload.request.headers.get('Content-Type')).toBe('image/jpeg');
    upload.flush({
      documentId: 'asset-1',
      scanStatus: 'Clean',
      storageStatus: 'Available',
      isCurrent: true,
    });
    http.expectOne('/api/v1/m08/evidence/pending').flush({ items: [] });
    http.expectOne(request => request.url.startsWith('/api/v1/m08/dispatch-board?date='))
      .flush({ unscheduled: [], pours: [], totals: {} });

    expect(page.actionDraft.trip.version).toBe(3);
    expect(page.actionDraft.trip.signatureReference).toBe('asset-1');
    expect(page.notice()).toContain('still unverified until supervisor review');
    http.verify();
  });
});

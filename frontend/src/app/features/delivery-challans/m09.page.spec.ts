import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M09Page } from './m09.page';

describe('M09Page', () => {
  it('loads the unbilled worklist from persisted challans', async () => {
    const api = { get: vi.fn(() => of({ items: [], total: 0 })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M09Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M09Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.get).toHaveBeenCalledWith(expect.stringContaining('/m09/challans?'));
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No persisted challans');
  });

  it('loads a persisted challan detail from the M09 HTTP contract', async () => {
    await TestBed.configureTestingModule({
      imports: [M09Page],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(M09Page);
    fixture.detectChanges();
    http.expectOne('/api/v1/m09/challans?page=1&pageSize=50&status=Delivered')
      .flush({ items: [{ challanId: 'challan-1', number: 'DC-1' }], total: 1 });

    fixture.componentInstance.open('challan-1');
    http.expectOne('/api/v1/m09/challans/challan-1').flush({
      challan: {
        challanId: 'challan-1',
        number: 'DC-1',
        status: 'Delivered',
        customerNameSnapshot: 'North Build',
        evidenceState: 'Unverified',
      },
      history: [],
      revisions: [],
    });
    fixture.detectChanges();

    expect(fixture.componentInstance.detail()?.challan.number).toBe('DC-1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('North Build');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Unverified');
    http.verify();
  });
});

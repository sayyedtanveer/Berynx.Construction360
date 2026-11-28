import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M19Page } from './m19.page';

describe('M19Page', () => {
  async function setup(postResult = of({})) {
    const api = {
      get: vi.fn((path: string) => {
        if (path === 'm19/trip-options') return of([]);
        if (path === 'm19/samples') return of([]);
        if (path === 'm19/reports') return of({ sampleCount: 0, dueOrOverdueTests: 0, reviewRequired: 0, passCount: 0, failCount: 0, passPercent: null, openDisputes: 0, closedDisputes: 0 });
        if (path === 'm19/criteria') return of([]);
        if (path === 'm19/disputes') return of([]);
        return of([]);
      }),
      post: vi.fn(() => postResult),
    };
    await TestBed.configureTestingModule({ imports: [M19Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }] }).compileComponents();
    const fixture = TestBed.createComponent(M19Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance, api, element: fixture.nativeElement as HTMLElement };
  }

  it('loads real M19 trip, sample and report endpoints and shows an honest empty state', async () => {
    const { page, api, element } = await setup();
    expect(api.get).toHaveBeenCalledWith('m19/trip-options');
    expect(api.get).toHaveBeenCalledWith('m19/samples');
    expect(page.report?.sampleCount).toBe(0);
    expect(element.textContent).toContain('No quality samples are recorded');
  });

  it('reports success only when the server confirms sample creation', async () => {
    const { page, api } = await setup();
    page.trips = [{ tripId: 'trip-1', number: 'TR-1', branchId: 'branch-1', completedAt: '2026-01-01T00:00:00Z', deliveredQuantity: 1, productNameSnapshot: 'Concrete', customerNameSnapshot: 'Customer', siteNameSnapshot: 'Site' }];
    page.sampleDraft.tripId = 'trip-1';
    page.sampleDraft.grade = 'M30';
    page.createSample();
    expect(api.post).toHaveBeenCalledWith('m19/samples', expect.objectContaining({ idempotencyKey: expect.any(String), tripId: 'trip-1', grade: 'M30' }));
    expect(page.message).toBe('Sample registered by the server.');
  });

  it('shows server recovery guidance instead of a false success on failed writes', async () => {
    const { page } = await setup(throwError(() => ({ error: { detail: 'A completed trip was not found.' } })));
    page.trips = [{ tripId: 'trip-1', number: 'TR-1', branchId: 'branch-1', completedAt: '2026-01-01T00:00:00Z', deliveredQuantity: 1, productNameSnapshot: 'Concrete', customerNameSnapshot: 'Customer', siteNameSnapshot: 'Site' }];
    page.sampleDraft.tripId = 'trip-1';
    page.sampleDraft.grade = 'M30';
    page.createSample();
    expect(page.message).toBe('');
    expect(page.error).toBe('A completed trip was not found.');
  });
});
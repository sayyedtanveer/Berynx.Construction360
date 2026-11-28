import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M07Page } from './m07.page';

describe('M07Page pagination', () => {
  it('reloads valid pages, resets filters, and stops at the known page boundaries', async () => {
    const listRequests: string[] = [];
    const api = {
      get: vi.fn((path: string) => {
        if (path.startsWith('/m07/purchase-orders?')) {
          listRequests.push(path);
          return of({ rows: [], total: 50, pageSize: 25 });
        }
        return of([]);
      }),
    };
    await TestBed.configureTestingModule({
      imports: [M07Page],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();

    const fixture = TestBed.createComponent(M07Page);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    const initialRequests = listRequests.length;

    page.nextPage();
    expect(page.page).toBe(2);
    expect(listRequests[listRequests.length - 1]).toContain('page=2');

    const lastPageRequests = listRequests.length;
    page.nextPage();
    expect(page.page).toBe(2);
    expect(listRequests).toHaveLength(lastPageRequests);

    page.previousPage();
    expect(page.page).toBe(1);
    expect(listRequests[listRequests.length - 1]).toContain('page=1');

    const firstPageRequests = listRequests.length;
    page.previousPage();
    expect(page.page).toBe(1);
    expect(listRequests).toHaveLength(firstPageRequests);

    page.page = 2;
    page.statusFilter = 'Approved';
    page.applyFilters();
    expect(page.page).toBe(1);
    expect(listRequests[listRequests.length - 1]).toContain('status=Approved');
    expect(listRequests.length).toBeGreaterThan(initialRequests);
  });
});
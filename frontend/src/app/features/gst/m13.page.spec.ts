import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M13Page } from './m13.page';

describe('M13Page', () => {
  it('loads workspace based on the selected month', async () => {
    const api = { get: vi.fn(() => of({ invoices: [], lines: [], exceptions: [], readinessPercent: 100 })), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M13Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M13Page);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.get).toHaveBeenCalledWith(expect.stringMatching('/m13/workspace\\?periodStart=\\d{4}-\\d{2}-01'));
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('External portal filing is not performed');
  });
});

import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M14Page } from './m14.page';

describe('M14Page', () => {
  it('loads categories and expense records through the API client', async () => {
    const api = { get: vi.fn(() => of([])), post: vi.fn(() => of({})) };
    await TestBed.configureTestingModule({
      imports: [M14Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M14Page);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(api.get).toHaveBeenCalledWith('/m14/categories');
    expect(api.get).toHaveBeenCalledWith('/m14/expenses');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('New expense');
  });
});

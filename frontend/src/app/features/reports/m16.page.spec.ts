import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M16Page } from './m16.page';

describe('M16Page', () => {
  it('loads the supported report catalogue', async () => {
    const api = { get: vi.fn(() => of({ available: ['R-S03'], pending: [] })), getBlob: vi.fn(() => of(new Blob())) };
    await TestBed.configureTestingModule({
      imports: [M16Page], providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(M16Page);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(api.get).toHaveBeenCalledWith('/m16/catalog');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Pending orders');
  });
});

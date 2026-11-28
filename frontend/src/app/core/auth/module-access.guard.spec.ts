import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../api/api-client.service';
import { moduleAccessGuard } from './module-access.guard';

describe('moduleAccessGuard', () => {
  const route = {
    data: { module: 'M10', permission: 'm10.invoices.view' },
  } as unknown as ActivatedRouteSnapshot;

  it('allows a route only when both the licensed module and view permission are present', async () => {
    const api = {
      get: vi.fn(() => of({
        modules: ['M10', 'M11', 'M12'],
        permissions: ['m10.invoices.view', 'm11.receipts.view', 'm12.reports.view'],
      })),
    };
    await TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();

    const result = await firstValueFrom(TestBed.runInInjectionContext(
      () => moduleAccessGuard(route, {} as RouterStateSnapshot),
    ) as Observable<boolean | UrlTree>);

    expect(result).toBe(true);
    expect(api.get).toHaveBeenCalledWith('/m02/context');
  });

  it('redirects when the module is unlicensed or the view permission is absent', async () => {
    const api = { get: vi.fn(() => of({ modules: ['M10'], permissions: [] })) };
    await TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const router = TestBed.inject(Router);

    const result = await firstValueFrom(TestBed.runInInjectionContext(
      () => moduleAccessGuard(route, {} as RouterStateSnapshot),
    ) as Observable<boolean | UrlTree>);

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toContain('access=denied');
  });

  it('fails closed when the server access contract is unavailable', async () => {
    const api = { get: vi.fn(() => throwError(() => new Error('offline'))) };
    await TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();
    const router = TestBed.inject(Router);

    const result = await firstValueFrom(TestBed.runInInjectionContext(
      () => moduleAccessGuard(route, {} as RouterStateSnapshot),
    ) as Observable<boolean | UrlTree>);

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toContain('access=unavailable');
  });
});

import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  afterEach(() => localStorage.clear());

  it('redirects unauthenticated navigation to login and preserves its return URL', async () => {
    const router = TestBed.inject(Router);
    const result = await firstValueFrom(TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/m03/customers' } as RouterStateSnapshot),
    ) as Observable<boolean | UrlTree>);

    expect(result).not.toBe(true);
    expect(router.serializeUrl(result as UrlTree))
      .toBe('/login?returnUrl=%2Fm03%2Fcustomers');
  });
});

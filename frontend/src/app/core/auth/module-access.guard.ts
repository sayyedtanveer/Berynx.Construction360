import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { ApiClient } from '../api/api-client.service';

interface NavigationAccess {
  modules: string[];
  permissions: string[];
}

export const moduleAccessGuard: CanActivateFn = (route) => {
  const api = inject(ApiClient);
  const router = inject(Router);
  const module = String(route.data['module'] ?? '');
  const permission = String(route.data['permission'] ?? '');
  return api.get<NavigationAccess>('/m02/context').pipe(
    map((access) =>
      access.modules.includes(module) && access.permissions.includes(permission)
        ? true
        : router.createUrlTree(['/'], { queryParams: { access: 'denied' } }),
    ),
    catchError(() => of(router.createUrlTree(['/'], { queryParams: { access: 'unavailable' } }))),
  );
};

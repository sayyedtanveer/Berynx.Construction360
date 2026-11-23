import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthSessionService } from './auth-session.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authSession = inject(AuthSessionService);
  const router = inject(Router);

  return authSession.isAuthenticated().pipe(
    map((authenticated) =>
      authenticated
        ? true
        : router.createUrlTree(['/'], {
            queryParams: { returnUrl: state.url },
          }),
    ),
  );
};

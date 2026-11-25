import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthSessionService } from './auth-session.service';

export const bearerAuthInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  const isAuthenticationRoute = request.url.includes('/m02/auth/');
  const token = session.accessToken();
  const authenticated = request.clone({
    withCredentials: true,
    ...(token && !isAuthenticationRoute
      ? { setHeaders: { Authorization: `Bearer ${token}` } }
      : {}),
  });

  return next(authenticated).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 ||
          isAuthenticationRoute || !token) {
        return throwError(() => error);
      }
      return session.refresh().pipe(
        catchError((refreshError: unknown) => {
          session.clear();
          if (!router.url.startsWith('/login')) {
            void router.navigate(['/login'], {
              queryParams: { returnUrl: router.url },
            });
          }
          return throwError(() => refreshError);
        }),
        switchMap((refreshed) =>
          next(request.clone({
            withCredentials: true,
            setHeaders: { Authorization: `Bearer ${refreshed}` },
          })),
        ),
      );
    }),
  );
};

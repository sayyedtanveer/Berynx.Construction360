import {
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { HttpErrorService } from './http-error.service';

interface ProblemDetailsResponse {
  detail?: string;
  title?: string;
  traceId?: string;
  extensions?: { traceId?: string };
}

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const errors = inject(HttpErrorService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const problem = error.error as ProblemDetailsResponse | null;
        const message =
          error.status === 0
            ? 'The service could not be reached. Check your connection and try again.'
            : problem?.detail ??
              problem?.title ??
              'The request could not be completed.';

        errors.report({
          message,
          traceId: problem?.traceId ?? problem?.extensions?.traceId,
        });
      }

      return throwError(() => error);
    }),
  );
};

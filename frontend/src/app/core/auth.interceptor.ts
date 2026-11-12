import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { AuthService } from './auth.service';

/**
 * Adds the bearer token to requests for our API only (never to other hosts) and ends the session
 * when the API says the token is no longer valid (401). Login/register 401s mean "wrong credentials", not "expired".
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const apiUrl = inject(API_BASE_URL);

  if (!request.url.startsWith(apiUrl)) {
    return next(request);
  }

  const token = auth.token();
  const authorizedRequest = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;
  const isCredentialsCheck = [`${apiUrl}/auth/login`, `${apiUrl}/auth/register`].includes(request.url);

  return next(authorizedRequest).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isCredentialsCheck) {
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};

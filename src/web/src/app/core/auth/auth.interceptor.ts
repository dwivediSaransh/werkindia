import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpEvent,
  HttpResponse,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, of, throwError, BehaviorSubject } from 'rxjs';
import { catchError, switchMap, finalize, filter, take } from 'rxjs/operators';
import { AuthSessionStore } from './auth-session.store';
import { AuthService, RefreshRequest, RefreshResponse } from './auth.service';

let isRefreshing = false;
let refreshSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<HttpEvent<unknown>> => {
  const store = inject(AuthSessionStore);
  const authService = inject(AuthService);

  // Skip auth header for non-REST calls (GraphQL goes through Apollo link)
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }

  const token = store.accessToken();
  if (!token) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      // Single-flight refresh: if already refreshing, wait for that result
      if (isRefreshing) {
        return refreshSubject.pipe(
          filter((result) => result !== null),
          take(1),
          switchMap((refreshResult) => {
            if (refreshResult === null) {
              return throwError(() => new Error('Refresh failed'));
            }
            // Retry original request with new token
            const retryReq = req.clone({
              setHeaders: { Authorization: `Bearer ${refreshResult}` },
            });
            return next(retryReq);
          }),
        );
      }

      isRefreshing = true;
      refreshSubject = new BehaviorSubject<string | null>(null);

      const refreshTokenHash = store.refreshTokenHash();
      const userId = store.userId();
      const deviceId = store.deviceId();

      if (!refreshTokenHash || !userId || !deviceId) {
        isRefreshing = false;
        refreshSubject.next(null);
        store.clearSession();
        return throwError(() => new Error('UNAUTHENTICATED'));
      }

      const refreshRequest: RefreshRequest = {
        userId,
        deviceId,
        refreshTokenHash,
      };

      return authService.refresh(refreshRequest).pipe(
        switchMap((response: RefreshResponse) => {
          // Update store with new tokens
          store.updateSession({
            accessToken: response.accessToken,
            refreshTokenHash: response.newRefreshTokenHash,
            expiresOn: new Date(response.expiresOn),
          });

          const newToken = response.accessToken;
          refreshSubject.next(newToken);
          isRefreshing = false;

          // Retry original request with new token
          const retryReq = req.clone({
            setHeaders: { Authorization: `Bearer ${newToken}` },
          });
          return next(retryReq);
        }),
        catchError((refreshError) => {
          isRefreshing = false;
          refreshSubject.next(null);
          store.clearSession();
          return throwError(() => refreshError);
        }),
        finalize(() => {
          // Ensure flags are reset
          isRefreshing = false;
        }),
      );
    }),
  );
};

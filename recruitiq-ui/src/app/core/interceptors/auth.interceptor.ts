import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { TokenService } from '../services/token.service';
import { AuthService } from '../services/auth.service';
import { AuthApiService } from '../../features/auth/services/auth-api.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const injector = inject(Injector);

  // Bypass interceptor logic for auth endpoints to prevent loops
  if (req.url.includes('/auth/login') || req.url.includes('/auth/refresh')) {
    return next(req);
  }

  const token = tokenService.getToken();
  let authReq = req;

  if (token) {
    authReq = addTokenHeader(req, token);
  }

  return next(authReq).pipe(
    catchError((error) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return handle401Error(authReq, next, tokenService, injector);
      }
      return throwError(() => error);
    })
  );
};

function addTokenHeader(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

function handle401Error(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
  tokenService: TokenService,
  injector: Injector
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    const authApiService = injector.get(AuthApiService);
    const authService = injector.get(AuthService);

    const accessToken = tokenService.getToken();
    const refreshToken = tokenService.getRefreshToken();

    if (accessToken && refreshToken) {
      return authApiService.refresh({ accessToken, refreshToken }).pipe(
        switchMap((response) => {
          isRefreshing = false;
          if (response.success && response.data) {
            const rememberMe = tokenService.getRememberMe();
            tokenService.saveTokens(
              response.data.accessToken,
              response.data.refreshToken,
              rememberMe
            );
            refreshTokenSubject.next(response.data.accessToken);
            return next(addTokenHeader(request, response.data.accessToken));
          }
          
          authService.logout();
          return throwError(() => new Error('Session expired. Please log in again.'));
        }),
        catchError((err) => {
          isRefreshing = false;
          authService.logout();
          return throwError(() => err);
        })
      );
    } else {
      isRefreshing = false;
      const authService = injector.get(AuthService);
      authService.logout();
      return throwError(() => new Error('Session expired. Please log in again.'));
    }
  } else {
    // Queue up requests until token refresh completes
    return refreshTokenSubject.pipe(
      filter((token) => token !== null),
      take(1),
      switchMap((token) => next(addTokenHeader(request, token!)))
    );
  }
}

import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError, from, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth';
import { ERROR_MESSAGES, MESSAGE_TITLES } from '../shared/constants/messages';

declare var Swal: any;

const REFRESH_BYPASS = ['Auth/RefreshToken', 'Auth/Login', 'Auth/Register', 'Auth/Logout', 'Auth/AzureAuthentication'];

function showError(userMessage: string): void {
  if (typeof Swal !== 'undefined') {
    Swal.fire({ icon: 'error', title: MESSAGE_TITLES.OOPS, text: userMessage });
  } else {
    alert(userMessage);
  }
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Suppress all error popups during logout to avoid "Something went wrong" flash
      if (authService.isLoggingOut) {
        return throwError(() => error);
      }

      if (error.error instanceof ErrorEvent) {
        console.error('Client Error:', error.error.message);
        showError(ERROR_MESSAGES.SOMETHING_WENT_WRONG);
        return throwError(() => error);
      }

      if (error.status === 401) {
        const isBypass = REFRESH_BYPASS.some(ep => req.url.includes(ep));
        if (!isBypass && authService.getRefreshTokenFromCache()) {
          return from(authService.attemptTokenRefresh()).pipe(
            switchMap((newToken) => {
              if (newToken) {
                const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } });
                return next(retryReq).pipe(
                  catchError((retryError: HttpErrorResponse) => {
                    if (retryError.status === 401) {
                      authService.logout();
                      showError(ERROR_MESSAGES.SESSION_EXPIRED);
                    }
                    return throwError(() => retryError);
                  })
                );
              }
              authService.logout();
              showError(ERROR_MESSAGES.SESSION_EXPIRED);
              return throwError(() => error);
            })
          );
        }

        authService.logout();
        showError(ERROR_MESSAGES.SESSION_EXPIRED);
        return throwError(() => error);
      }

      if (error.status === 403) {
        showError(ERROR_MESSAGES.FORBIDDEN);
      } else {
        showError(ERROR_MESSAGES.SOMETHING_WENT_WRONG);
      }

      const details = error.error?.detail || error.error?.message ||
        (typeof error.error === 'string' ? error.error : error.message || 'Unknown server error');
      console.error('API Error:', { url: req.url, status: error.status, statusText: error.statusText, details });

      return throwError(() => error);
    })
  );
};

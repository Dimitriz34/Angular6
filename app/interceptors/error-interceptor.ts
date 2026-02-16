import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth';
import { ERROR_MESSAGES, MESSAGE_TITLES } from '../shared/constants/messages';

declare var Swal: any;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // User-friendly message to display in popup
      let userMessage = ERROR_MESSAGES.SOMETHING_WENT_WRONG;
      
      // Technical details for console logging only
      let technicalDetails = '';
      
      if (error.error instanceof ErrorEvent) {
        // Client-side error
        technicalDetails = `Client Error: ${error.error.message}`;
      } else {
        // Server-side error
        if (error.status === 401) {
          authService.logout();
          userMessage = ERROR_MESSAGES.SESSION_EXPIRED;
        } else if (error.status === 403) {
          userMessage = ERROR_MESSAGES.FORBIDDEN;
        }
        
        // Capture technical details for logging
        if (error.error && error.error.detail) {
          technicalDetails = error.error.detail;
        } else if (error.error && error.error.message) {
          technicalDetails = error.error.message;
        } else if (error.error && typeof error.error === 'string') {
          technicalDetails = error.error;
        } else {
          technicalDetails = error.message || 'Unknown server error';
        }
      }

      // Log technical details to console for debugging (not shown to user)
      console.error('API Error:', {
        url: req.url,
        status: error.status,
        statusText: error.statusText,
        details: technicalDetails
      });

      if (typeof Swal !== 'undefined') {
        Swal.fire({
          icon: 'error',
          title: MESSAGE_TITLES.OOPS,
          text: userMessage,
        });
      } else {
        alert(userMessage);
      }

      return throwError(() => error);
    })
  );
};

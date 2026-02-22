import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth';
import { from } from 'rxjs';
import { switchMap } from 'rxjs/operators';

// Endpoints that should bypass the auth waiting logic (public endpoints)
const PUBLIC_ENDPOINTS = [
  'Auth/Login',
  'Auth/Register',
  'Auth/AzureAuthentication',
  'Auth/ApplicationAuthentication',
  'Auth/RefreshToken',
  'Auth/Logout'
];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  
  // Check if this is a public endpoint that doesn't need auth waiting
  const isPublicEndpoint = PUBLIC_ENDPOINTS.some(endpoint => req.url.includes(endpoint));
  
  if (isPublicEndpoint) {
    // For public endpoints, just add the api-version header and proceed immediately
    const authReq = req.clone({
      setHeaders: {
        'api-version': '1.0',
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      }
    });
    return next(authReq);
  }
  
  // For protected endpoints, wait for AuthService to be ready
  return from(authService.isAuthenticated()).pipe(
    switchMap(() => {
      // Clone request and add api-version header
      const headers: { [key: string]: string } = {
        'api-version': '1.0',
        'Accept': 'application/json'
      };
      
      // Only set Content-Type for non-FormData requests
      // FormData sets its own Content-Type with boundary automatically
      if (!(req.body instanceof FormData)) {
        headers['Content-Type'] = 'application/json';
      }
      
      // Get token from AuthService cache (it's populated after isAuthenticated() resolves)
      const token = authService.getTokenFromCache();
      
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      // Bind logged-in user's ID in request header for backend identification
      const userId = authService.getUserIdFromCache();
      if (userId) {
        headers['userId'] = userId;
      }
      
      const authReq = req.clone({
        setHeaders: headers
      });
      
      return next(authReq);
    })
  );
};
import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth';

export const authGuard: CanActivateFn = async (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  try {
    // Ensure the auth service has completed initialization
    const isAuth = await authService.isAuthenticated();
    
    if (isAuth) {
      // Additional validation: verify token is still valid
      const token = authService.getTokenFromCache();
      if (!token) {
        // Token was cleared or invalid, redirect to login
        router.navigate(['/Account/Login'], { queryParams: { returnUrl: state.url } });
        return false;
      }
      return true;
    }

    // Not authenticated, redirect to login with return URL
    router.navigate(['/Account/Login'], { queryParams: { returnUrl: state.url } });
    return false;
  } catch (error) {
    // On any error, redirect to login for safety
    router.navigate(['/Account/Login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
};

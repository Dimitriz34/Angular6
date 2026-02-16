import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth';

export const roleGuard: CanActivateFn = async (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  try {
    const expectedRoles = route.data['roles'] as Array<string>;
    const userRole = await authService.getRole();
    const isAuth = await authService.isAuthenticated();
    const token = authService.getTokenFromCache();

    // Check if user is authenticated and has a valid token
    if (!isAuth || !token) {
      // Session expired or invalid, redirect to login with return URL
      router.navigate(['/Account/Login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    // Check if user has the required role (case-insensitive comparison)
    const userRoleUpper = (userRole || '').toUpperCase();
    const hasRequiredRole = expectedRoles.some(role => role.toUpperCase() === userRoleUpper);
    
    if (hasRequiredRole) {
      return true;
    }

    // User is authenticated but doesn't have required role
    // Redirect to appropriate dashboard based on their role
    const dashboard = userRoleUpper === 'ADMIN' ? '/Admin/Dashboard' : '/User/Dashboard';
    router.navigate([dashboard]);
    return false;
  } catch (error) {
    // On any error, redirect to login for safety
    router.navigate(['/Account/Login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
};

import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth';

declare var Swal: any;

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
      router.navigate(['/Account/Login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    // Check if user has the required role (case-insensitive comparison)
    const userRoleUpper = (userRole || '').toUpperCase();
    const hasRequiredRole = expectedRoles.some(role => role.toUpperCase() === userRoleUpper);
    
    if (hasRequiredRole) {
      // Check approval status — flag is sourced from tpm_user.active via JWT
      const approved = await authService.isApproved();
      if (!approved) {
        if (typeof Swal !== 'undefined') {
          Swal.fire({
            icon: 'warning',
            title: 'Access Restricted',
            text: 'You do not have access to this module. Please contact your administrator to request access.',
            confirmButtonColor: '#704294'
          });
        }
        router.navigate(['/Dashboard']);
        return false;
      }
      return true;
    }

    // User is authenticated but doesn't have required role
    const dashboard = userRoleUpper === 'ADMIN' ? '/Admin/Dashboard' : '/User/Dashboard';
    router.navigate([dashboard]);
    return false;
  } catch (error) {
    router.navigate(['/Account/Login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
};

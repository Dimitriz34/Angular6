import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, from, of, firstValueFrom } from 'rxjs';
import { map, mergeMap } from 'rxjs/operators';
import { BaseService } from './api';
import { SecureStorageService } from './secure-storage.service';
import { ApiResponse, ResultCodes } from '../models/api-response.model';
import { logoutAzure } from '../auth';

// Role IDs matching backend RoleType enum
export enum RoleId {
  UNKNOWN = 0,
  MODERATOR = 1,
  USER = 2,
  ADMIN = 3,
  TESTER = 4
}

type SessionCache = {
  token: string | null;
  refreshToken: string | null;
  role: string | null;
  roleId: number | null;
  email: string | null;
  userId: string | null;
  approved: boolean;
};

@Injectable({
  providedIn: 'root',
})
export class AuthService extends BaseService {
  private router = inject(Router);
  private secureStorage = inject(SecureStorageService);
  private cache: SessionCache = { token: null, refreshToken: null, role: null, roleId: null, email: null, userId: null, approved: false };
  private ready: Promise<void> | null = null;
  private initialized = false;
  private refreshTimer: ReturnType<typeof setTimeout> | null = null;
  private refreshInProgress: Promise<string | null> | null = null;
  private _loggingOut = false;

  /**
   * True while logout is in progress.
   * Used by the error interceptor to suppress network error popups
   * that naturally occur when tokens are cleared mid-flight.
   */
  get isLoggingOut(): boolean {
    return this._loggingOut;
  }

  constructor() {
    super();
  }

  async initializeSession(): Promise<void> {
    // Prevent multiple initializations
    if (this.initialized) {
      return;
    }
    this.initialized = true;
    
    // Quick check: if there's no session data at all, return immediately
    const hasEncryptedData = Object.keys(sessionStorage).some(k => k.startsWith('__encrypted_'));
    const hasLegacyData = sessionStorage.getItem('Auth') !== null;
    
    if (!hasEncryptedData && !hasLegacyData) {
      // No session data, no need to wait for restoration
      return;
    }
    
    // Only restore if there's data
    this.ready = this.restoreFromStorage();
    
    const timeout = new Promise<void>((resolve) => {
      setTimeout(() => {
        console.warn('AuthService: Session restoration timed out, proceeding without cached session');
        resolve();
      }, 3000);
    });
    
    await Promise.race([this.ready, timeout]);
  }

  login(credentials: any): Observable<any> {
    return this.post<ApiResponse<any>>('Auth/Login', credentials).pipe(
      mergeMap((response: ApiResponse<any>) => {
        if (response && response.resultCode === ResultCodes.SUCCESS) {
          const tokenData = response.resultData && response.resultData.length > 0 ? response.resultData[0] : null;
          if (tokenData) {
            const accessToken = typeof tokenData === 'string' ? tokenData : tokenData?.accessToken;
            const refreshToken = typeof tokenData === 'string' ? null : tokenData?.refreshToken;
            const expiresIn = typeof tokenData === 'string' ? 1800 : (tokenData?.expiresIn ?? 1800);
            if (accessToken) {
              return from(this.setSession(accessToken, refreshToken, expiresIn)).pipe(map(() => response));
            }
          }
        }
        return of(response);
      })
    );
  }

  azureLogin(email: string, upn: string, displayName?: string, username?: string): Observable<any> {
    return this.post<ApiResponse<any>>('Auth/AzureAuthentication', { 
      Email: email,
      Upn: upn,
      DisplayName: displayName,
      Username: username
    }).pipe(
      mergeMap((response: ApiResponse<any>) => {
        if (response && response.resultCode === ResultCodes.SUCCESS) {
          const tokenData = response.resultData && response.resultData.length > 0 ? response.resultData[0] : null;
          const accessToken = typeof tokenData === 'string' ? tokenData : tokenData?.accessToken;
          const refreshToken = typeof tokenData === 'string' ? null : tokenData?.refreshToken;
          const expiresIn = typeof tokenData === 'string' ? 1800 : (tokenData?.expiresIn ?? 1800);
          if (accessToken) {
            return from(this.setSession(accessToken, refreshToken, expiresIn)).pipe(map(() => response));
          }
        }
        return of(response);
      })
    );
  }

  private async restoreFromStorage(): Promise<void> {
    try {
      // Quick check: if there's nothing in storage, skip the async crypto operations
      const hasEncryptedData = Object.keys(sessionStorage).some(k => k.startsWith('__encrypted_'));
      const hasLegacyData = sessionStorage.getItem('Auth') !== null;
      
      if (!hasEncryptedData && !hasLegacyData) {
        // No session data to restore, exit early
        return;
      }

      // Try to restore encrypted claims and token from sessionStorage
      const role = await this.secureStorage.getItem<string>('Role', true);
      const email = await this.secureStorage.getItem<string>('LoginEmail', true);
      const userId = await this.secureStorage.getItem<string>('UserId', true);
      const token = await this.secureStorage.getItem<string>('Auth', true);

      if (token) {
        await this.cacheFromToken(token);
        const rt = sessionStorage.getItem('RefreshToken');
        if (rt) this.cache.refreshToken = rt;
        this.scheduleRefresh();
      }

      if (role || email || userId) {
        this.cache.role = role || this.cache.role;
        this.cache.email = email || this.cache.email;
        this.cache.userId = userId || this.cache.userId;
        return;
      }

      // Migration: check for legacy unencrypted token
      const legacyToken = sessionStorage.getItem('Auth');
      if (legacyToken) {
        await this.cacheFromToken(legacyToken);
        // Store claims encrypted, clean up legacy unencrypted storage
        await Promise.all([
          this.secureStorage.setItem('Role', this.cache.role, true),
          this.secureStorage.setItem('LoginEmail', this.cache.email, true),
          this.secureStorage.setItem('UserId', this.cache.userId, true)
        ]);
        sessionStorage.removeItem('Auth');
        sessionStorage.removeItem('Role');
        sessionStorage.removeItem('LoginEmail');
        sessionStorage.removeItem('UserId');
      }
    } catch (error) {
      this.cache = { token: null, refreshToken: null, role: null, roleId: null, email: null, userId: null, approved: false };
    }
  }

  private async setSession(token: string, refreshToken?: string | null, expiresIn?: number): Promise<void> {
    try {
      await this.cacheFromToken(token);
      sessionStorage.setItem('Auth', token);
      sessionStorage.setItem('Role', this.cache.role || '');
      sessionStorage.setItem('RoleId', this.cache.roleId?.toString() || '');
      sessionStorage.setItem('LoginEmail', this.cache.email || '');
      sessionStorage.setItem('UserId', this.cache.userId || '');
      sessionStorage.setItem('Approved', this.cache.approved ? '1' : '0');

      if (refreshToken) {
        this.cache.refreshToken = refreshToken;
        sessionStorage.setItem('RefreshToken', refreshToken);
      }

      this.scheduleRefresh(expiresIn);
    } catch (error) {
      // Continue anyway - the cache is already populated
    }
  }

  private async cacheFromToken(token: string): Promise<void> {
    try {
      // Ensure token is a valid JWT format (contains exactly 2 dots)
      const parts = token.split('.');
      if (parts.length !== 3) {
        this.cache = { token: null, refreshToken: null, role: null, roleId: null, email: null, userId: null, approved: false };
        return;
      }

      if (!this.isTokenValid(token)) {
        this.cache = { token: null, refreshToken: null, role: null, roleId: null, email: null, userId: null, approved: false };
        return;
      }

      this.cache.token = token;
      const payload = JSON.parse(atob(parts[1]));
      const role = payload['role'] || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const roleIdStr = payload['roleId'];
      this.cache.role = role;
      this.cache.roleId = roleIdStr ? parseInt(roleIdStr, 10) : null;
      this.cache.email = payload['unique_name'] || payload['name'];
      this.cache.userId = payload['nameid'] || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
      this.cache.approved = payload['approved'] === '1' || payload['approved'] === 1;
    } catch (error) {
      this.cache = { token: null, refreshToken: null, role: null, roleId: null, email: null, userId: null, approved: false };
    }
  }

  private isTokenValid(token: string): boolean {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) {
        return false;
      }

      const payload = JSON.parse(atob(parts[1]));
      const exp = payload['exp'];

      if (!exp) {
        // No expiration claim, consider it valid
        return true;
      }

      // Check if token has expired (exp is in seconds, Date.now() is in milliseconds)
      const currentTime = Math.floor(Date.now() / 1000);
      return exp > currentTime;
    } catch (error) {
      return false;
    }
  }

  async updateLoginEmail(email: string): Promise<void> {
    this.cache.email = email;
    await this.secureStorage.setItem('LoginEmail', email, true);
  }

  register(userData: any): Observable<any> {
    return this.post<ApiResponse<string>>('Auth/Register', userData);
  }

  private scheduleRefresh(expiresIn?: number): void {
    if (this.refreshTimer) clearTimeout(this.refreshTimer);
    if (!this.cache.refreshToken) return;

    let seconds = expiresIn ?? 1800;
    if (!expiresIn && this.cache.token) {
      try {
        const payload = JSON.parse(atob(this.cache.token.split('.')[1]));
        if (payload.exp) seconds = payload.exp - Math.floor(Date.now() / 1000);
      } catch { /* use default */ }
    }

    const refreshMs = Math.max((seconds - 120) * 1000, 10_000);
    this.refreshTimer = setTimeout(() => this.attemptTokenRefresh(), refreshMs);
  }

  async attemptTokenRefresh(): Promise<string | null> {
    if (this.refreshInProgress) return this.refreshInProgress;

    const rt = this.cache.refreshToken || sessionStorage.getItem('RefreshToken');
    if (!rt) return null;

    this.refreshInProgress = (async () => {
      try {
        const response = await firstValueFrom(
          this.post<ApiResponse<any>>('Auth/RefreshToken', { RefreshToken: rt })
        );
        if (response?.resultCode === ResultCodes.SUCCESS && response.resultData?.length > 0) {
          const data = response.resultData[0];
          await this.setSession(data.accessToken, data.refreshToken, data.expiresIn);
          return data.accessToken as string;
        }
        return null;
      } catch {
        return null;
      } finally {
        this.refreshInProgress = null;
      }
    })();

    return this.refreshInProgress;
  }

  getRefreshTokenFromCache(): string | null {
    if (!this.cache.refreshToken) {
      this.cache.refreshToken = sessionStorage.getItem('RefreshToken');
    }
    return this.cache.refreshToken;
  }

  async logout(): Promise<void> {
    this._loggingOut = true;

    // 1. Revoke the refresh token on the backend (best-effort)
    const rt = this.cache.refreshToken || sessionStorage.getItem('RefreshToken');
    if (rt) {
      try {
        await firstValueFrom(this.post<any>('Auth/Logout', { RefreshToken: rt }));
      } catch { /* best-effort */ }
    }

    // 2. Stop scheduled token refresh
    if (this.refreshTimer) { clearTimeout(this.refreshTimer); this.refreshTimer = null; }

    // 3. Clear in-memory session cache
    this.cache = { token: null, refreshToken: null, role: null, roleId: null, email: null, userId: null, approved: false };
    this.initialized = false;

    // 4. Clear app-specific sessionStorage keys (preserve MSAL state for clean Azure logout)
    const appKeys = ['Auth', 'Role', 'RoleId', 'LoginEmail', 'UserId', 'Approved', 'RefreshToken', 'userInfo'];
    appKeys.forEach(key => sessionStorage.removeItem(key));
    Object.keys(sessionStorage)
      .filter(k => k.startsWith('__encrypted_'))
      .forEach(key => sessionStorage.removeItem(key));

    // 5. Clear localStorage (app data only — MSAL uses sessionStorage)
    localStorage.clear();

    // 6. Clear browser caches
    if ('caches' in window) {
      try {
        const cacheNames = await caches.keys();
        await Promise.all(cacheNames.map(name => caches.delete(name)));
      } catch { /* ignore */ }
    }

    // 7. Azure AD logout redirect — MSAL reads its own state, clears it, then redirects.
    //    After redirect, the app reloads fresh and _loggingOut resets naturally.
    await logoutAzure();
  }

  async isAuthenticated(): Promise<boolean> {
    // Return from cache immediately if available
    if (this.cache.token) {
      return true;
    }
    // Try plain sessionStorage
    const token = sessionStorage.getItem('Auth');
    if (token) {
      await this.cacheFromToken(token);
      return true;
    }
    return false;
  }

  async getRole(): Promise<string | null> {
    // Return from cache immediately if available (e.g., after login)
    if (this.cache.role) {
      return this.cache.role;
    }
    // Try plain sessionStorage
    const storedRole = sessionStorage.getItem('Role');
    if (storedRole) {
      this.cache.role = storedRole;
      return storedRole;
    }
    return null;
  }

  async getRoleId(): Promise<number | null> {
    // Return from cache immediately if available
    if (this.cache.roleId !== null) {
      return this.cache.roleId;
    }
    // Try plain sessionStorage
    const storedRoleId = sessionStorage.getItem('RoleId');
    if (storedRoleId) {
      this.cache.roleId = parseInt(storedRoleId, 10);
      return this.cache.roleId;
    }
    return null;
  }

  /**
   * Check if user is admin using roleId first, fallback to case-insensitive string comparison
   */
  async isAdmin(): Promise<boolean> {
    try {
      // First try by roleId (more reliable)
      const roleId = await this.getRoleId();
      if (roleId !== null) {
        return roleId === RoleId.ADMIN;
      }
      
      // Fallback to case-insensitive string comparison
      const role = await this.getRole();
      return role?.toUpperCase() === 'ADMIN';
    } catch (error) {
      // Last resort fallback
      const role = await this.getRole();
      return role?.toUpperCase() === 'ADMIN';
    }
  }

  async getEmail(): Promise<string | null> {
    // Return from cache immediately if available
    if (this.cache.email) {
      return this.cache.email;
    }
    // Try plain sessionStorage
    const storedEmail = sessionStorage.getItem('LoginEmail');
    if (storedEmail) {
      this.cache.email = storedEmail;
      return storedEmail;
    }
    return null;
  }

  async getUserId(): Promise<string | null> {
    // Return from cache immediately if available
    if (this.cache.userId) {
      return this.cache.userId;
    }
    // Try plain sessionStorage
    const storedUserId = sessionStorage.getItem('UserId');
    if (storedUserId) {
      this.cache.userId = storedUserId;
      return storedUserId;
    }
    return null;
  }

  getUserInfo(): Observable<ApiResponse<any>> {
    return this.get<ApiResponse<any>>('User/GetUserInfo');
  }

  // Synchronous getter for token from cache (used by interceptor)
  // Returns null if no token in cache (e.g., on page refresh)
  getTokenFromCache(): string | null {
    if (!this.cache.token) {
      this.cache.token = sessionStorage.getItem('Auth');
    }
    return this.cache.token;
  }

  /**
   * Check if user is approved (active=1 in database, carried via JWT 'approved' claim)
   * This is the database-driven access control flag
   */
  async isApproved(): Promise<boolean> {
    if (this.cache.token) {
      return this.cache.approved;
    }
    
    const storedApproved = sessionStorage.getItem('Approved');
    if (storedApproved !== null) {
      this.cache.approved = storedApproved === '1';
      return this.cache.approved;
    }
    
    const token = sessionStorage.getItem('Auth');
    if (token) {
      await this.cacheFromToken(token);
      return this.cache.approved;
    }
    
    return false;
  }

  /**
   * Synchronous getter for userId from cache (used by interceptor for header binding)
   */
  getUserIdFromCache(): string | null {
    if (!this.cache.userId) {
      this.cache.userId = sessionStorage.getItem('UserId');
    }
    return this.cache.userId;
  }
}

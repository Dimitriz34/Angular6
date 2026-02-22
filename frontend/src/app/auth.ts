import { PublicClientApplication, Configuration, LogLevel, BrowserCacheLocation, RedirectRequest, AuthenticationResult } from '@azure/msal-browser';
import { environment } from '../environments/environment';

export const msalConfig: Configuration = {
  auth: {
    clientId: environment.azureConfig.clientId,
    authority: environment.azureConfig.authority,
    redirectUri: environment.azureConfig.redirectUri,
    postLogoutRedirectUri: environment.azureConfig.baseUrl,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.SessionStorage,
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          case LogLevel.Info:
            console.info(message);
            return;
          case LogLevel.Verbose:
            console.debug(message);
            return;
          case LogLevel.Warning:
            console.warn(message);
            return;
        }
      },
      logLevel: LogLevel.Info,
    },
  },
};

export const msalInstance = new PublicClientApplication(msalConfig);

// Helper to extract email/UPN from claims (upn has priority over preferred_username)
export function getEmailFromClaims(claims: any): string | null {
  // upn = User Principal Name (needs to be configured as optional claim in Azure AD)
  // preferred_username = default email in v2.0 tokens
  return claims?.upn || claims?.preferred_username || null;
}

// Handle redirect response after returning from Azure AD
export async function handleRedirectResponse(): Promise<AuthenticationResult | null> {
  await msalInstance.initialize();
  return msalInstance.handleRedirectPromise();
}

export async function loginWithAzure(): Promise<void> {
  await msalInstance.initialize();

  const loginRequest: RedirectRequest = {
    scopes: ['user.read', 'openid', 'profile'],
  };

  // Use redirect instead of popup - navigates in same page
  await msalInstance.loginRedirect(loginRequest);
}

export async function logoutAzure(): Promise<void> {
  try {
    await msalInstance.initialize();

    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0) {
      // MSAL handles clearing its own sessionStorage state during logoutRedirect
      await msalInstance.logoutRedirect({
        postLogoutRedirectUri: window.location.origin + '/login',
        account: accounts[0],
      });
    } else {
      // No Azure session exists — clear any stale MSAL cache keys and navigate
      clearMsalSessionState();
      window.location.href = '/login';
    }
  } catch {
    // MSAL logout failed — force-clear everything and redirect
    clearMsalSessionState();
    window.location.href = '/login';
  }
}

/**
 * Removes all MSAL-related keys from sessionStorage.
 * Only used as a fallback when MSAL's own logout cannot run.
 */
function clearMsalSessionState(): void {
  const msalKeys = Object.keys(sessionStorage).filter(
    k => k.startsWith('msal.') || k.includes('login.windows.net') || k.includes('msal')
  );
  msalKeys.forEach(k => sessionStorage.removeItem(k));
}

import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth';
import { ApplicationService } from '../../services/application';
import { loginWithAzure, handleRedirectResponse } from '../../auth';
import { ToastrService } from 'ngx-toastr';
import { IMAGE_PATHS } from '../../shared/constants/image-paths';
import { ResultCodes } from '../../models/api-response.model';
import { AUTH_MESSAGE_FORMATTERS, AUTH_MESSAGES, MESSAGE_TITLES } from '../../shared/constants/messages';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent implements OnInit {
  public readonly IMAGE_PATHS = IMAGE_PATHS;
  private authService = inject(AuthService);
  private applicationService = inject(ApplicationService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  userLogin = {
    Email: '',
    Password: ''
  };
  errorMessage = '';

  async ngOnInit() {
    // Handle redirect response after returning from Azure AD
    try {
      const response = await handleRedirectResponse();
      if (response && response.account) {
        await this.processAzureLogin(response);
      }
    } catch (error) {
      this.toastr.error(AUTH_MESSAGES.AZURE_LOGIN_FAILED_RETRY, MESSAGE_TITLES.AUTHENTICATION_ERROR);
    }
  }

  private async processAzureLogin(response: any) {
    // Extract claims from idTokenClaims
    const claims = response.idTokenClaims || {};
    
    // UPN - User Principal Name (keep as-is, may have #EXT# format for external users)
    const upn = claims['upn'] || response.account.username;
    
    if (!upn) {
      this.toastr.error(AUTH_MESSAGES.AZURE_MISSING_EMAIL, MESSAGE_TITLES.AUTHENTICATION_ERROR);
      return;
    }

    try {
      console.log('Fetching Azure AD user info for UPN:', upn);
      // Call SearchADUsers API to get actual Azure AD user data
      const adUserResponse = await firstValueFrom(this.applicationService.searchADUsers(upn));
      
      console.log('SearchADUsers response:', adUserResponse);
      
      if (!adUserResponse || !adUserResponse.users || adUserResponse.users.length === 0) {
        console.error('No users found in SearchADUsers response');
        this.toastr.error('Unable to fetch user information from Azure AD', MESSAGE_TITLES.AUTHENTICATION_ERROR);
        return;
      }

      const adUser = adUserResponse.users[0];
      console.log('AD User data:', adUser);
      
      // Extract data from Azure AD response - MUST use displayName from Azure AD
      const email = adUser.mail || adUser.userPrincipalName;
      const displayName = adUser.displayName || (adUser.givenName && adUser.surname ? `${adUser.givenName} ${adUser.surname}` : '');
      const username = email.includes('@') ? email.split('@')[0] : displayName;
      
      console.log('Extracted: email=', email, ', displayName=', displayName);
      
      // Send to backend: Email = actual email, Upn = UPN, DisplayName = full name, Username = short name
      this.authService.azureLogin(email, upn, displayName, username).subscribe({
        next: async (res) => {
          if (res && res.resultCode === ResultCodes.SUCCESS) {
            // Always call SearchADUsers after authentication to refresh Azure AD data
            let refreshedUser = adUser;
            try {
              const refreshedResponse = await firstValueFrom(this.applicationService.searchADUsers(upn));
              if (refreshedResponse && refreshedResponse.users && refreshedResponse.users.length > 0) {
                refreshedUser = refreshedResponse.users[0];
              }
            } catch (refreshError) {
              console.warn('SearchADUsers after authentication failed:', refreshError);
            }

            const refreshedEmail = refreshedUser.mail || refreshedUser.userPrincipalName || email;
            const refreshedDisplayName = refreshedUser.displayName || (refreshedUser.givenName && refreshedUser.surname ? `${refreshedUser.givenName} ${refreshedUser.surname}` : displayName);
            const refreshedUsername = refreshedEmail.includes('@') ? refreshedEmail.split('@')[0] : refreshedDisplayName;

            // Store user info in localStorage for entire session
            const userInfo = {
              displayName: refreshedDisplayName,
              email: refreshedEmail,
              userPrincipalName: upn,
              username: refreshedUsername,
              givenName: refreshedUser.givenName || '',
              surname: refreshedUser.surname || '',
              id: refreshedUser.id,
              mail: refreshedUser.mail,
              jobTitle: refreshedUser.jobTitle || '',
              department: refreshedUser.department || '',
              officeLocation: refreshedUser.officeLocation || '',
              mobilePhone: refreshedUser.mobilePhone || '',
              companyName: refreshedUser.companyName || ''
            };

            localStorage.setItem('userInfo', JSON.stringify(userInfo));
            console.log('User info stored in localStorage:', userInfo);
            
            this.toastr.success(
              AUTH_MESSAGES.AZURE_LOGIN_SUCCESS,
              AUTH_MESSAGE_FORMATTERS.authorizedUser(refreshedDisplayName)
            );
            // Navigate to common dashboard for all roles
            this.router.navigate(['/Dashboard']);
          } else {
            const message = res.resultMessages && res.resultMessages.length > 0 
              ? res.resultMessages[0] 
              : AUTH_MESSAGES.AZURE_LOGIN_FAILED_DEFAULT;
            this.toastr.error(message, MESSAGE_TITLES.AUTHENTICATION_FAILED);
            
            if (res.resultCode === ResultCodes.USER_NOT_APPROVED) {
              this.router.navigate(['/NoAccess']);
            }
          }
        },
        error: (err) => {
          console.error('Azure login error:', err);
          this.toastr.error(AUTH_MESSAGES.AZURE_LOGIN_FAILED_RETRY, MESSAGE_TITLES.AUTHENTICATION_ERROR);
        }
      });
    } catch (error) {
      console.error('SearchADUsers error:', error);
      this.toastr.error('Unable to authenticate with Azure AD', MESSAGE_TITLES.AUTHENTICATION_ERROR);
    }
  }

  async onAzureLogin() {
    try {
      // This will redirect to Azure AD in the same page
      await loginWithAzure();
    } catch (error) {
      this.toastr.error(AUTH_MESSAGES.AZURE_LOGIN_FAILED_RETRY, MESSAGE_TITLES.AUTHENTICATION_ERROR);
    }
  }

  onSubmit(event: Event) {
    event.preventDefault();
    this.authService.login(this.userLogin).subscribe({
      next: async (response) => {
        if (response && response.resultCode === ResultCodes.SUCCESS) {
          // Store basic user info for regular login (will be enriched by layout component)
          sessionStorage.setItem('userInfo', JSON.stringify({
            displayName: this.userLogin.Email.split('@')[0],
            email: this.userLogin.Email,
            userPrincipalName: this.userLogin.Email,
            username: this.userLogin.Email.split('@')[0]
          }));
          
          this.toastr.success(AUTH_MESSAGES.LOGIN_SUCCESS, MESSAGE_TITLES.AUTHENTICATED);
          const isAdmin = await this.authService.isAdmin();
          // Navigate to common dashboard for all roles
          this.router.navigate(['/Dashboard']);
        } else {
          // Handle business logic failures using resultCode and resultMessages
          const message = response.resultMessages && response.resultMessages.length > 0 
            ? response.resultMessages[0] 
            : AUTH_MESSAGES.LOGIN_FAILED_DEFAULT;
          this.errorMessage = message;
          this.toastr.error(message, MESSAGE_TITLES.LOGIN_FAILED);
          
          // Special handling for unapproved users
          if (response.resultCode === ResultCodes.USER_NOT_APPROVED) {
            this.router.navigate(['/NoAccess']);
          }
          
          // Special handling for Azure AD users trying to use password login
          if (response.resultCode === ResultCodes.AZURE_AD_LOGIN_REQUIRED) {
            this.toastr.info('Please use the "Login with Azure AD" button below.', 'Azure AD Account');
          }
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.detail || err.error?.message || err.message || AUTH_MESSAGES.LOGIN_ERROR;
        this.toastr.error(this.errorMessage, MESSAGE_TITLES.LOGIN_FAILED);
      }
    });
  }
}
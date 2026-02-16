import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth';
import { ToastrService } from 'ngx-toastr';
import { IMAGE_PATHS } from '../../shared/constants/image-paths';
import { ResultCodes } from '../../models/api-response.model';
import { MESSAGE_TITLES, REGISTRATION_MESSAGES } from '../../shared/constants/messages';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent {
  public readonly IMAGE_PATHS = IMAGE_PATHS;
  private authService = inject(AuthService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  registrationData = {
    Email: '',
    Username: '',
    Upn: '',
    AppSecret: '',
    ConfirmPassword: ''
  };
  errorMessage = '';
  isLoading = false;

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    
    // Validation
    if (!this.registrationData.Email?.trim()) {
      this.errorMessage = REGISTRATION_MESSAGES.EMAIL_REQUIRED;
      this.toastr.warning(this.errorMessage, MESSAGE_TITLES.VALIDATION_ERROR);
      return;
    }

    if (!this.registrationData.Username?.trim()) {
      this.errorMessage = REGISTRATION_MESSAGES.USERNAME_REQUIRED;
      this.toastr.warning(this.errorMessage, MESSAGE_TITLES.VALIDATION_ERROR);
      return;
    }

    if (!this.registrationData.Upn?.trim()) {
      this.errorMessage = REGISTRATION_MESSAGES.UPN_REQUIRED;
      this.toastr.warning(this.errorMessage, MESSAGE_TITLES.VALIDATION_ERROR);
      return;
    }

    if (this.registrationData.AppSecret !== this.registrationData.ConfirmPassword) {
      this.errorMessage = REGISTRATION_MESSAGES.PASSWORD_MISMATCH;
      this.toastr.warning(this.errorMessage, MESSAGE_TITLES.VALIDATION_ERROR);
      return;
    }

    if (!this.registrationData.AppSecret?.trim()) {
      this.errorMessage = REGISTRATION_MESSAGES.PASSWORD_REQUIRED;
      this.toastr.warning(this.errorMessage, MESSAGE_TITLES.VALIDATION_ERROR);
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    try {
      const response = await firstValueFrom(this.authService.register(this.registrationData));
      // Check resultCode for success
      if (response && response.resultCode === ResultCodes.REGISTRATION_SUCCESS) {
        this.toastr.success(
          REGISTRATION_MESSAGES.REGISTRATION_SUCCESS,
          MESSAGE_TITLES.ACCOUNT_CREATED,
          { timeOut: 5000 }
        );
        // Delay navigation to allow user to see the success message
        setTimeout(() => {
          this.router.navigate(['/Account/Login']);
        }, 2000);
      } else {
        // Handle other resultCodes
        const message = response.resultMessages && response.resultMessages.length > 0 
          ? response.resultMessages[0] 
          : REGISTRATION_MESSAGES.REGISTRATION_FAILED;
        this.errorMessage = message;
        this.toastr.error(message, MESSAGE_TITLES.REGISTRATION_FAILED, { timeOut: 6000 });
      }
    } catch (err: any) {
      const errorDetail = 
        err.error?.detail || 
        err.error?.message || 
        err.error?.Message ||
        err.message || 
        REGISTRATION_MESSAGES.REGISTRATION_ERROR;
      this.errorMessage = errorDetail;
      this.toastr.error(errorDetail, MESSAGE_TITLES.REGISTRATION_FAILED, { timeOut: 6000 });
    } finally {
      this.isLoading = false;
    }
  }
}
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { UserService } from '../../services/user';
import { AuthService } from '../../services/auth';
import { Router } from '@angular/router';
import { MESSAGE_TITLES, PASSWORD_MESSAGES } from '../../shared/constants/messages';

declare var Swal: any;

@Component({
  selector: 'app-password-update',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './password-update.html',
  styleUrl: './password-update.scss'
})
export class PasswordUpdateComponent implements OnInit {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private router = inject(Router);

  updateData: any = {
    email: '',
    appSecret: '',
    confirmPassword: '',
    userId: ''
  };

  message: string = '';
  error: string = '';

  async ngOnInit(): Promise<void> {
    this.updateData.email = await this.authService.getEmail();
    this.updateData.userId = await this.authService.getUserId();
  }

  async onSubmit(): Promise<void> {
    if (this.updateData.appSecret !== this.updateData.confirmPassword) {
      this.error = PASSWORD_MESSAGES.PASSWORD_MISMATCH;
      return;
    }

    const payload = {
      email: this.updateData.email,
      userId: this.updateData.userId,
      appSecret: this.updateData.appSecret,
      modifiedBy: this.updateData.userId,
      modificationDateTime: new Date().toISOString()
    };

    try {
      await firstValueFrom(this.userService.updatePassword(payload));
      await Swal.fire({
        icon: 'success',
        title: MESSAGE_TITLES.UPDATED,
        text: PASSWORD_MESSAGES.PASSWORD_UPDATE_SUCCESS,
        showConfirmButton: false,
        timer: 2500
      });
      this.message = PASSWORD_MESSAGES.PASSWORD_UPDATE_SUCCESS;
      this.error = '';
      this.updateData.appSecret = '';
      this.updateData.confirmPassword = '';
    } catch (err: any) {
      this.error = err.error?.detail || PASSWORD_MESSAGES.PASSWORD_UPDATE_ERROR;
      this.message = '';
    }
  }
}

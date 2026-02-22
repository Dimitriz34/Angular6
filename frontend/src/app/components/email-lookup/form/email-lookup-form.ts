import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../services/auth';
import { EmailLookupService } from '../../../services/email-lookup';
import { ToastrService } from 'ngx-toastr';
import { EMAIL_LOOKUP_MESSAGE_FORMATTERS, EMAIL_LOOKUP_MESSAGES, MESSAGE_TITLES } from '../../../shared/constants/messages';

@Component({
  selector: 'app-email-lookup-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './email-lookup-form.html',
  styleUrl: './email-lookup-form.scss'
})
export class EmailLookupFormComponent implements OnInit {
  private authService = inject(AuthService);
  private emailLookupService = inject(EmailLookupService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  lookupData: any = {
    id: 0,
    serviceName: '',
    type: 'FREE',
    active: true,
    createdBy: '',
    creationDateTime: new Date().toISOString(),
    modifiedBy: '',
    modificationDateTime: new Date().toISOString()
  };

  isEdit: boolean = false;
  error: string = '';

  setType(type: string) {
    this.lookupData.type = type;
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('Id');
    if (id) {
      this.isEdit = true;
      this.loadEmailService(+id);
    } else {
      this.lookupData.createdBy = await this.authService.getUserId();
    }
  }

  async loadEmailService(id: number): Promise<void> {
    try {
      const response = await firstValueFrom(this.emailLookupService.getEmailServiceById(id));
      if (response && response.resultData && response.resultData.length > 0) {
        this.lookupData = response.resultData[0];
        this.lookupData.modifiedBy = await this.authService.getUserId();
      }
    } catch (err: any) {
      this.toastr.error(EMAIL_LOOKUP_MESSAGES.LOAD_ERROR, MESSAGE_TITLES.ERROR);
    }
  }

  async onSubmit(): Promise<void> {
    try {
      if (this.isEdit) {
        await firstValueFrom(this.emailLookupService.updateEmailService(this.lookupData));
        this.toastr.success(
          EMAIL_LOOKUP_MESSAGE_FORMATTERS.updateSuccess(this.lookupData.serviceName),
          MESSAGE_TITLES.SUCCESS
        );
      } else {
        await firstValueFrom(this.emailLookupService.addEmailService(this.lookupData));
        this.toastr.success(
          EMAIL_LOOKUP_MESSAGE_FORMATTERS.saveSuccess(this.lookupData.serviceName),
          MESSAGE_TITLES.SUCCESS
        );
      }
      this.router.navigate(['/Admin/Lookup/EmailServices/List']);
    } catch (err: any) {
      this.error = err.error?.detail || (this.isEdit ? EMAIL_LOOKUP_MESSAGES.UPDATE_ERROR : EMAIL_LOOKUP_MESSAGES.SAVE_ERROR);
      this.toastr.error(this.error, MESSAGE_TITLES.ERROR);
    }
  }
}

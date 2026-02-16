import { Component, Input, Output, EventEmitter, inject, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmailService } from '../../../services/email';
import { EmailDetail } from '../../../models/email.model';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-email-detail-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './email-detail-modal.html',
  styleUrl: './email-detail-modal.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmailDetailModalComponent implements OnChanges {
  @Input() emailId: string | null = null;
  @Input() isOpen: boolean = false;
  @Output() closeModal = new EventEmitter<void>();

  private emailService = inject(EmailService);
  private cdr = inject(ChangeDetectorRef);

  emailDetail: EmailDetail | null = null;
  isLoading: boolean = false;
  errorMessage: string = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['emailId'] && this.emailId && this.isOpen) {
      this.loadEmailDetail();
    }
    if (changes['isOpen'] && this.isOpen && this.emailId) {
      this.loadEmailDetail();
    }
  }

  async loadEmailDetail(): Promise<void> {
    if (!this.emailId) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    try {
      const response: any = await firstValueFrom(this.emailService.getEmailDetail(this.emailId));
      if (response && response.resultData && response.resultData.length > 0) {
        this.emailDetail = response.resultData[0];
      } else {
        this.errorMessage = 'Email not found';
      }
    } catch (error: any) {
      this.errorMessage = error?.message || 'Failed to load email details';
    } finally {
      this.isLoading = false;
      this.cdr.markForCheck();
    }
  }

  close(): void {
    this.emailDetail = null;
    this.errorMessage = '';
    this.closeModal.emit();
  }

  getRecipientTypeLabel(type: string): string {
    switch (type?.toLowerCase()) {
      case '0':
      case 'to':
        return 'To';
      case '1':
      case 'cc':
        return 'CC';
      case '2':
      case 'bcc':
        return 'BCC';
      default:
        return type || 'To';
    }
  }

  getStatusClass(status: string | undefined): string {
    switch (status?.toLowerCase()) {
      case 'sent':
        return 'status-sent';
      case 'pending':
        return 'status-pending';
      case 'failed':
        return 'status-failed';
      default:
        return 'status-unknown';
    }
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  }
}

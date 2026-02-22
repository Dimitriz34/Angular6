import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef, SecurityContext } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';
import { DomSanitizer } from '@angular/platform-browser';
import { environment } from '../../../../environments/environment';
import { EmailService } from '../../../services/email';
import { ApplicationService } from '../../../services/application';
import { AuthService } from '../../../services/auth';
import { SecureStorageService } from '../../../services/secure-storage.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { EmailDetailModalComponent } from '../detail/email-detail-modal';
import { Email, EmailPost } from '../../../models/email.model';
import { Application } from '../../../models/application.model';
import { ToastrService } from 'ngx-toastr';
import { switchMap, tap, catchError, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { of, Subject, firstValueFrom } from 'rxjs';
import { EMAIL_MESSAGES, MESSAGE_TITLES } from '../../../shared/constants/messages';

@Component({
  selector: 'app-email-list',
  standalone: true,
  imports: [CommonModule, PaginationComponent, FormsModule, NgSelectModule, EmailDetailModalComponent],
  templateUrl: './email-list.html',
  styleUrl: './email-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmailListComponent implements OnInit {
  private emailService = inject(EmailService);
  private applicationService = inject(ApplicationService);
  private authService = inject(AuthService);
  private secureStorage = inject(SecureStorageService);
  private toastr = inject(ToastrService);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);
  private sanitizer = inject(DomSanitizer);

  emailList: Email[] = [];
  applicationList: Application[] = [];
  currentPage: number = 1;
  pageSize: number = 5;
  count: number = 0;
  searchTerm: string = '';
  selectedAppName: string = '';
  viewMode: 'list' | 'card' = 'list';
  
  // Date Range Filters
  startDate: string = '';
  endDate: string = '';

  // Test Email Form
  showTestForm: boolean = false;
  isSending: boolean = false;
  isDirectAccess: boolean = false; // True when user navigates directly without appId query param
  useTPInternalConfig: boolean = false; // When true, use TP Internal email config instead of app
  emailData: EmailPost = {
    subject: '',
    body: '',
    isHtml: false,
    toRecipients: '',
    ccRecipients: '',
    appId: null,
    appPassword: '',
    smtpUserEmail: '',
    useTPAssist: false
  };
  selectedFiles: File[] = [];

  // TP Data Assist Preview
  isEnhancing: boolean = false;
  tpAssistPreview: string = '';
  showTPAssistPreview: boolean = false;

  // Email Detail Modal
  showEmailDetailModal: boolean = false;
  selectedEmailId: string | null = null;

  // Debounced search
  private searchSubject = new Subject<string>();

  // To/CC Recipients search
  toRecipientsSearchInput$ = new Subject<string>();
  toRecipientsLoading: boolean = false;
  adUsersForRecipients: any[] = [];
  selectedToRecipients: any[] = [];

  ccRecipientsSearchInput$ = new Subject<string>();
  ccRecipientsLoading: boolean = false;
  adUsersForCCRecipients: any[] = [];
  selectedCCRecipients: any[] = [];

  // Allow adding custom email (manual entry)
  addCustomRecipient = (term: string) => {
    // Validate if it looks like an email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (emailRegex.test(term)) {
      return { displayName: term, mail: term };
    }
    return null; // Don't allow non-email entries
  };

  async ngOnInit(): Promise<void> {
    // Setup debounced search first
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(() => {
      this.onSearch();
    });

    // Setup To/CC recipients search
    this.setupRecipientsSearch();

    // Check for appId in query params FIRST (before loading data)
    // This ensures emailData.appId is set when applications load
    const params = this.route.snapshot.queryParams;
    if (params['appId']) {
      this.emailData.appId = +params['appId'];
      this.showTestForm = true;
      this.isDirectAccess = false; // Coming from app registration flow
    } else if (params['action'] === 'send') {
      // Open test form when action=send is passed (from TP Data Assist)
      this.showTestForm = true;
      this.isDirectAccess = true;
      // Pre-populate for TP Data Assist navigation
      this.prepopulateTestEmailForTPAssist();
    } else if (params['date']) {
      // Handle date filter (e.g., "show me emails from yesterday")
      this.handleDateQueryParam(params['date']);
    } else {
      this.isDirectAccess = true; // User navigated directly to this page
    }

    // Now load data
    await this.loadEmails();
    await this.loadApplications();

    // Subscribe to queryParams for subsequent changes (navigation while on same page)
    this.route.queryParams.subscribe(queryParams => {
      const appId = queryParams['appId'];
      const action = queryParams['action'];
      
      if (appId && this.emailData.appId !== +appId) {
        this.emailData.appId = +appId;
        this.showTestForm = true;
        
        if (this.applicationList.length > 0) {
          this.onApplicationChange();
        }
        this.cdr.markForCheck();
      }
      
      // Handle action=send for opening test email form (from TP Data Assist)
      if (action === 'send' && !this.showTestForm) {
        this.showTestForm = true;
        this.isDirectAccess = true;
        this.cdr.markForCheck();
      }
      
      // Handle date filter changes
      if (queryParams['date'] && !appId) {
        this.handleDateQueryParam(queryParams['date']);
        this.loadEmails();
      }
    });
  }

  /**
   * Handle date query parameter for filtering emails
   * Supports: today, yesterday, last7days, last30days, thismonth, or specific date (YYYY-MM-DD)
   */
  private handleDateQueryParam(dateParam: string): void {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    switch (dateParam.toLowerCase()) {
      case 'today':
        this.startDate = this.formatDate(today);
        this.endDate = this.formatDate(today);
        break;
      case 'yesterday':
        const yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);
        this.startDate = this.formatDate(yesterday);
        this.endDate = this.formatDate(yesterday);
        break;
      case 'last7days':
        const last7 = new Date(today);
        last7.setDate(last7.getDate() - 7);
        this.startDate = this.formatDate(last7);
        this.endDate = this.formatDate(today);
        break;
      case 'last30days':
        const last30 = new Date(today);
        last30.setDate(last30.getDate() - 30);
        this.startDate = this.formatDate(last30);
        this.endDate = this.formatDate(today);
        break;
      case 'thismonth':
        const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
        this.startDate = this.formatDate(firstOfMonth);
        this.endDate = this.formatDate(today);
        break;
      default:
        // Assume it's a specific date in YYYY-MM-DD format
        if (/^\d{4}-\d{2}-\d{2}$/.test(dateParam)) {
          this.startDate = dateParam;
          this.endDate = dateParam;
        }
        break;
    }
    
    if (this.startDate) {
      this.toastr.info(`Filtering emails by date: ${dateParam}`, MESSAGE_TITLES.INFO);
    }
    this.cdr.markForCheck();
  }

  private formatDate(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  private setupRecipientsSearch(): void {
    // To Recipients typeahead with debounce
    this.toRecipientsSearchInput$.pipe(
      tap(term => {
        if (term && term.length >= 3) {
          this.toRecipientsLoading = true;
          this.cdr.markForCheck();
        }
      }),
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(term => {
        if (!term || term.length < 3) {
          this.toRecipientsLoading = false;
          this.adUsersForRecipients = [];
          this.cdr.markForCheck();
          return of({ users: [] });
        }
        return this.applicationService.searchADUsers(term).pipe(
          catchError(() => of({ users: [] }))
        );
      })
    ).subscribe(response => {
      this.toRecipientsLoading = false;
      if (response && response.users) {
        this.adUsersForRecipients = response.users.filter((u: any) => u.mail).map((u: any) => ({ ...u, photoUrl: null }));
        this.adUsersForRecipients.forEach((u: any) => this.loadRecipientPhoto(u, 'to'));
      }
      this.cdr.markForCheck();
    });

    // CC Recipients typeahead with debounce
    this.ccRecipientsSearchInput$.pipe(
      tap(term => {
        if (term && term.length >= 3) {
          this.ccRecipientsLoading = true;
          this.cdr.markForCheck();
        }
      }),
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(term => {
        if (!term || term.length < 3) {
          this.ccRecipientsLoading = false;
          this.adUsersForCCRecipients = [];
          this.cdr.markForCheck();
          return of({ users: [] });
        }
        return this.applicationService.searchADUsers(term).pipe(
          catchError(() => of({ users: [] }))
        );
      })
    ).subscribe(response => {
      this.ccRecipientsLoading = false;
      if (response && response.users) {
        this.adUsersForCCRecipients = response.users.filter((u: any) => u.mail).map((u: any) => ({ ...u, photoUrl: null }));
        this.adUsersForCCRecipients.forEach((u: any) => this.loadRecipientPhoto(u, 'cc'));
      }
      this.cdr.markForCheck();
    });
  }

  // Load user photo for recipients (similar to application-form)
  loadRecipientPhoto(user: any, type: 'to' | 'cc'): void {
    this.applicationService.getADUserPhoto(user.userPrincipalName).subscribe(res => {
      if (res?.success && res?.data) {
        // API returns data:image/jpeg;base64,... format directly
        user.photoUrl = res.data;
        if (type === 'to') this.adUsersForRecipients = [...this.adUsersForRecipients];
        else this.adUsersForCCRecipients = [...this.adUsersForCCRecipients];
        this.cdr.markForCheck();
      }
    });
  }

  // Handle recipient selection
  onToRecipientSelect(items: any[]): void {
    this.selectedToRecipients = items || [];
    this.emailData.toRecipients = this.selectedToRecipients.map(item => item.mail || item).join(',');
    this.cdr.markForCheck();
  }

  onCCRecipientSelect(items: any[]): void {
    this.selectedCCRecipients = items || [];
    this.emailData.ccRecipients = this.selectedCCRecipients.map(item => item.mail || item).join(',');
    this.cdr.markForCheck();
  }

  compareRecipientEmails(a: any, b: any): boolean {
    if (!a || !b) return a === b;
    return a?.mail === b?.mail || a === b;
  }

  async loadEmails(): Promise<void> {
    try {
      const response: any = await firstValueFrom(
        this.emailService.getEmails(this.currentPage, this.pageSize, this.searchTerm, this.selectedAppName, this.startDate || undefined, this.endDate || undefined)
      );
      if (response && (response.data || response.resultData)) {
        const emails = response.data || (response.resultData && response.resultData.length > 0 ? response.resultData : []);
        this.emailList = emails;
        this.count = response.totalRecords || 0;
      }
      // FIX: Trigger change detection for OnPush strategy
      this.cdr.markForCheck();
    } catch (error) {
      this.cdr.markForCheck();
    }
  }

  async onSearch(): Promise<void> {
    this.currentPage = 1;
    await this.loadEmails();
  }

  // Debounced search trigger
  onSearchInput(term: string) {
    this.searchTerm = term;
    this.searchSubject.next(term);
  }

  async onAppFilterChange(): Promise<void> {
    this.currentPage = 1;
    await this.loadEmails();
  }

  async loadApplications(): Promise<void> {
    try {
      const role = await this.authService.getRole();
      let response: any;
      
      if (role === 'USER') {
        const userId = await this.authService.getUserId();
        if (userId) {
          response = await firstValueFrom(this.applicationService.getUserApplications(userId, 1, 100));
        } else {
          response = await firstValueFrom(this.applicationService.getApplications(1, 100));
        }
      } else {
        response = await firstValueFrom(this.applicationService.getApplications(1, 100));
      }
      
      if (response && (response.data || response.resultData)) {
        const applications = response.data || (response.resultData && response.resultData.length > 0 ? response.resultData : []);
        this.applicationList = applications;
        
        // If appId was set via query params, trigger application change to load SMTP user email
        if (this.emailData.appId) {
          this.onApplicationChange();
        }
      }
      // FIX: Trigger change detection
      this.cdr.markForCheck();
    } catch (error) {
      this.cdr.markForCheck();
    }
  }

  async onPageChange(page: number): Promise<void> {
    this.currentPage = page;
    await this.loadEmails();
  }

  toggleTestForm() {
    this.showTestForm = !this.showTestForm;
  }

  /**
  * Pre-populate test email form when navigating from TP Data Assist
   */
  private prepopulateTestEmailForTPAssist(): void {
    // Enable TP Internal config toggle
    this.useTPInternalConfig = true;
    
    // Pre-fill recipient with current user's email
    const userInfoStr = sessionStorage.getItem('userInfo');
    if (userInfoStr) {
      try {
        const userInfo = JSON.parse(userInfoStr);
        const email = userInfo.email || '';
        if (email) {
          this.emailData.toRecipients = email;
          // Also add to the recipients dropdown selection
          this.selectedToRecipients = [{
            displayName: userInfo.displayName || email,
            mail: email
          }];
          this.adUsersForRecipients = [...this.selectedToRecipients];
        }
      } catch (e) {
        // Silently handle error
      }
    }
    
    // Set default test email template
    this.emailData.subject = 'Test Email from TP Mailer';
    this.emailData.body = `Hello,

This is a test email sent from TP Mailer to verify that the email configuration is working correctly.

If you received this email, your setup is complete and functioning properly.

Best regards,
TP Mailer System`;
    this.emailData.isHtml = false;
    
    this.cdr.markForCheck();
  }

  onFileChange(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      this.selectedFiles = Array.from(event.target.files);
    }
  }

  // Handle TP Internal Config toggle change
  onTPInternalToggle() {
    if (this.useTPInternalConfig) {
      // Clear app-specific fields when using TP Internal
      this.emailData.appId = null;
      this.emailData.smtpUserEmail = '';
      this.emailData.appPassword = '';
    }
    this.syncTPAssistAvailability();
    this.cdr.markForCheck();
  }

  onApplicationChange() {
    const appId = this.emailData.appId;
    
    if (appId) {
      const selectedApp = this.applicationList.find(app => app.id.toString() === appId.toString());
      if (selectedApp) {
        // Check if this is an internal app using the isInternalApp flag
        const isInternalApp = selectedApp.isInternalApp;
        
        if (isInternalApp) {
          // For TP Internal apps, from email is configured during registration
          this.emailData.smtpUserEmail = selectedApp.fromEmailAddress || 'tp-internal@managed.local';
          this.emailData.appPassword = '';
          this.toastr.info(EMAIL_MESSAGES.TP_SERVICES_DETECTED, MESSAGE_TITLES.INFO);
        } else {
          // For external apps, use the fromEmailAddress
          this.emailData.smtpUserEmail = selectedApp.fromEmailAddress || '';
          this.emailData.appPassword = '';
        }
      }
    } else {
      this.emailData.smtpUserEmail = '';
      this.emailData.appPassword = '';
    }
    this.syncTPAssistAvailability();
    // FIX: Trigger change detection for OnPush strategy after updating emailData
    this.cdr.markForCheck();
  }

  isTPAssistAvailable(): boolean {
    // Allow TP Assist when user explicitly chooses TP Internal config (no app selection)
    if (this.useTPInternalConfig) {
      return true;
    }
    if (!this.emailData.appId) {
      return false;
    }
    const selectedApp = this.applicationList.find((app: any) => app.id.toString() === this.emailData.appId?.toString());
    return !!selectedApp?.useTPAssist;
  }

  private resetTPAssistState(): void {
    this.emailData.useTPAssist = false;
    this.showTPAssistPreview = false;
    this.tpAssistPreview = '';
    this.isEnhancing = false;
  }

  private syncTPAssistAvailability(): void {
    if (!this.isTPAssistAvailable()) {
      this.resetTPAssistState();
    }
  }

  async sendTestEmail() {
    if (this.emailData.useTPAssist && !this.isTPAssistAvailable()) {
      this.resetTPAssistState();
      this.toastr.warning('TP Data Assist is not enabled for the selected application.', MESSAGE_TITLES.WARNING);
    }

    // Check if using TP Internal config (toggle) or if selected app is internal
    const isUsingTPInternal = this.useTPInternalConfig;
    const selectedApp = !isUsingTPInternal && this.emailData.appId 
      ? this.applicationList.find(app => app.id.toString() === this.emailData.appId?.toString())
      : null;
    const isInternalApp = isUsingTPInternal || selectedApp?.isInternalApp || false;
    
    // Validate required fields
    // For TP Internal config: only need subject and toRecipients
    // For regular apps: need appId, smtpUserEmail, subject, toRecipients
    if (!this.emailData.subject || !this.emailData.toRecipients) {
      this.toastr.warning(EMAIL_MESSAGES.REQUIRED_FIELDS, MESSAGE_TITLES.WARNING);
      return;
    }
    
    // appId and smtpUserEmail required when NOT using TP Internal config
    if (!isUsingTPInternal && (!this.emailData.appId || !this.emailData.smtpUserEmail)) {
      this.toastr.warning(EMAIL_MESSAGES.REQUIRED_FIELDS, MESSAGE_TITLES.WARNING);
      return;
    }
    
    // AppPassword required for non-internal apps
    if (!isInternalApp && !this.emailData.appPassword) {
      this.toastr.warning(EMAIL_MESSAGES.APP_PASSWORD_REQUIRED, MESSAGE_TITLES.WARNING);
      return;
    }

    this.isSending = true;
    
    // For TP Internal config, use appId=0 for API call
    const apiAppId = isUsingTPInternal ? 0 : this.emailData.appId;
    const emailDataForApi = { ...this.emailData, appId: apiAppId };
    
    // Store AppPassword for guidance email in sessionStorage (skip for TP Internal)
    if (this.emailData.appId && !isUsingTPInternal) {
      await this.secureStorage.setItem(`app_${this.emailData.appId}_appPassword`, this.emailData.appPassword || '', true);
    }
    
    this.emailService.sendEmail(emailDataForApi, this.selectedFiles).pipe(
      switchMap((response: any) => {
        this.toastr.success(EMAIL_MESSAGES.TEST_EMAIL_SUCCESS, MESSAGE_TITLES.SUCCESS);
        
        // Only attempt guidance email if coming from app creation flow (credentials stored in session)
        // Skip for TP Internal config and skip silently if credentials not available
        if (this.emailData.appId && !isUsingTPInternal && !isInternalApp) {
          const appId = +this.emailData.appId;
          // Explicitly fetch all necessary application details as per architectural direction
          return this.applicationService.getApplicationById(appId).pipe(
            switchMap(async (appDetails: any) => {
              const appInfo = appDetails?.data || appDetails;
              try {
                const ownerEmail = await this.secureStorage.getItem<string>(`app_${appId}_ownerEmail`, true);
                const appPassword = await this.secureStorage.getItem<string>(`app_${appId}_appPassword`, true);
                const appSecret = await this.secureStorage.getItem<string>(`app_${appId}_appSecret`, true);
                const coOwnerEmail = await this.secureStorage.getItem<string>(`app_${appId}_coOwnerEmail`, true);
                
                // Silently skip guidance email if credentials not available (not from app creation flow)
                if (!ownerEmail || !appSecret || appPassword === null) {
                  return of(response);
                }
                
                this.secureStorage.removeItem(`app_${appId}_ownerEmail`, true);
                this.secureStorage.removeItem(`app_${appId}_appPassword`, true);
                this.secureStorage.removeItem(`app_${appId}_appSecret`, true);
                this.secureStorage.removeItem(`app_${appId}_coOwnerEmail`, true);
                
                const baseApiUrl = environment.apiUrl.replace(/\/api\/?$/, '');
                await this.applicationService.sendGuidanceEmail(appId, ownerEmail, appPassword, appSecret, baseApiUrl, coOwnerEmail || undefined).toPromise();
                this.toastr.info(EMAIL_MESSAGES.GUIDANCE_SUCCESS, MESSAGE_TITLES.INFO);
                return of(response);
              } catch (error: any) {
                const guidanceError = error?.error?.message || EMAIL_MESSAGES.GUIDANCE_FAILED;
                this.toastr.error(guidanceError, MESSAGE_TITLES.ERROR);
                return of(response);
              }
            }),
            catchError((error: any) => {
              return of(response);
            })
          );
        }
        return of(response);
      }),
      catchError((error: any) => {
        const errorMsg = error.error?.detail || EMAIL_MESSAGES.SEND_EMAIL_ERROR;
        this.toastr.error(errorMsg, MESSAGE_TITLES.ERROR);
        this.isSending = false;
        return of(null);
      })
    ).subscribe({
      next: (response: any) => {
        this.isSending = false;
        this.showTestForm = false;
        this.showTPAssistPreview = false;
        this.tpAssistPreview = '';
        this.loadEmails();
        // Reset form
        this.emailData = {
          subject: '',
          body: '',
          isHtml: false,
          toRecipients: '',
          ccRecipients: '',
          appId: '',
          appPassword: '',
          smtpUserEmail: '',
          useTPAssist: false
        };
        this.selectedFiles = [];
        this.selectedToRecipients = [];
        this.selectedCCRecipients = [];
        this.useTPInternalConfig = false;
        this.cdr.markForCheck();
      },
      error: (err: any) => {
        const errorMsg = err.error?.detail || EMAIL_MESSAGES.SEND_EMAIL_ERROR;
        this.toastr.error(errorMsg, MESSAGE_TITLES.ERROR);
        this.isSending = false;
      }
    });
  }

  /**
  * Preview TP Data Assist enhancement - calls GetTPAssist API and displays result
   */
  async previewTPAssist(): Promise<void> {
    if (!this.isTPAssistAvailable() || !this.emailData.useTPAssist) {
      this.toastr.warning('TP Data Assist is not enabled for the selected application.', MESSAGE_TITLES.WARNING);
      return;
    }

    if (!this.emailData.body) {
      this.toastr.warning('Email body is required for TP Data Assist preview', MESSAGE_TITLES.WARNING);
      return;
    }

    this.isEnhancing = true;
    this.showTPAssistPreview = false;
    this.tpAssistPreview = '';
    this.cdr.markForCheck();

    try {
      const response: any = await firstValueFrom(
        this.emailService.getTPAssist({
          body: this.emailData.body,
          subject: this.emailData.subject,
          isHtml: this.emailData.isHtml
        })
      );

      if (response?.resultCode === 1 && response?.resultData?.[0]?.success) {
        const rawHtml = response.resultData[0].body;
        this.tpAssistPreview = this.sanitizer.sanitize(SecurityContext.HTML, rawHtml) || '';
        this.showTPAssistPreview = true;
        this.toastr.success('TP Data Assist enhancement preview ready', MESSAGE_TITLES.SUCCESS);
      } else {
        const errorMsg = response?.resultData?.[0]?.errorMessage || response?.resultMessages?.[0] || 'TP Data Assist enhancement failed';
        this.toastr.error(errorMsg, MESSAGE_TITLES.ERROR);
      }
    } catch (error: any) {
      const errorMsg = error?.error?.detail || 'Failed to get TP Data Assist preview';
      this.toastr.error(errorMsg, MESSAGE_TITLES.ERROR);
    } finally {
      this.isEnhancing = false;
      this.cdr.markForCheck();
    }
  }

  /**
  * Apply TP Data Assist preview to email body
   */
  applyTPAssistPreview(): void {
    if (this.tpAssistPreview) {
      this.emailData.body = this.tpAssistPreview;
      this.showTPAssistPreview = false;
      this.toastr.info('TP Data Assist enhancement applied to email body', MESSAGE_TITLES.INFO);
      this.cdr.markForCheck();
    }
  }

  getToRecipientsDisplay(): string {
    if (!this.selectedToRecipients || this.selectedToRecipients.length === 0) {
      return 'No recipients';
    }
    return this.selectedToRecipients.map(r => r.mail || r.displayName).join(', ');
  }

  getCCRecipientsDisplay(): string {
    if (!this.selectedCCRecipients || this.selectedCCRecipients.length === 0) {
      return '';
    }
    return this.selectedCCRecipients.map(r => r.mail || r.displayName).join(', ');
  }

  // Compare function for ng-select to handle type coercion (number vs string)
  // When bindValue is used, ng-select passes the full item object as first param
  compareAppId = (item: any, selected: any): boolean => {
    if (item === null || selected === null || item === undefined || selected === undefined) {
      return false;
    }
    // item is the full object when bindValue is used, so we need to extract the id
    const itemId = typeof item === 'object' ? item?.id : item;
    return itemId?.toString() === selected?.toString();
  };

  isInternalApp(): boolean {
    // If using TP Internal config toggle, it's internal
    if (this.useTPInternalConfig) {
      return true;
    }
    if (!this.emailData.appId) {
      return false;
    }
    const selectedApp = this.applicationList.find((app: any) => app.id.toString() === this.emailData.appId?.toString());
    return selectedApp?.isInternalApp || false;
  }

  // Custom form validation that handles internal apps (no password required)
  isFormValid(): boolean {
    // Always required: toRecipients and subject
    if (!this.emailData.toRecipients || !this.emailData.subject) {
      return false;
    }
    
    // If using TP Internal config, only toRecipients and subject are required
    if (this.useTPInternalConfig) {
      return true;
    }
    
    // For non-TP Internal: also require appId and smtpUserEmail
    if (!this.emailData.appId || !this.emailData.smtpUserEmail) {
      return false;
    }
    
    // For external apps, appPassword is required
    if (!this.isInternalApp() && !this.emailData.appPassword) {
      return false;
    }
    
    return true;
  }

  // Export emails to CSV
  exportToCSV() {
    if (!this.emailList || this.emailList.length === 0) {
      this.toastr.warning(EMAIL_MESSAGES.EXPORT_EMPTY, MESSAGE_TITLES.WARNING);
      return;
    }

    // Prepare CSV headers
    const headers = EMAIL_MESSAGES.CSV_HEADERS;
    
    // Prepare CSV rows
    const rows = this.emailList.map(email => [
      email.upn || '',
      email.username || '',
      email.appName || '',
      email.sender || '',
      this.escapeCSV(email.subject || ''),
      this.escapeCSV(email.body || ''),
      email.serviceName || '',
      new Date(email.creationDateTime).toLocaleDateString() || '',
      new Date(email.creationDateTime).toLocaleTimeString() || '',
      email.active === 1 ? EMAIL_MESSAGES.CSV_STATUS_DELIVERED : EMAIL_MESSAGES.CSV_STATUS_FAILED
    ]);

    // Create CSV content
    const csvContent = [
      headers.map(h => this.escapeCSV(h)).join(','),
      ...rows.map(row => row.map(cell => this.escapeCSV(String(cell))).join(','))
    ].join('\n');

    // Create blob and download
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `Email_Logs_${new Date().getTime()}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    this.toastr.success(EMAIL_MESSAGES.EXPORT_SUCCESS, MESSAGE_TITLES.SUCCESS);
  }

  // Helper method to escape CSV values
  private escapeCSV(value: string): string {
    if (value.includes(',') || value.includes('"') || value.includes('\n')) {
      return `"${value.replace(/"/g, '""')}"`;
    }
    return value;
  }

  // TrackBy functions for better performance with *ngFor
  trackByEmailId(index: number, email: any): string {
    return email.emailId || index;
  }

  trackByAppId(index: number, app: any): number {
    return app.id || index;
  }

  setViewMode(mode: 'list' | 'card') {
    this.viewMode = mode;
    this.cdr.markForCheck();
  }

  async clearFilters(): Promise<void> {
    this.searchTerm = '';
    this.selectedAppName = '';
    this.startDate = '';
    this.endDate = '';
    this.currentPage = 1;
    await this.loadEmails();
  }

  async onDateFilterChange(): Promise<void> {
    this.currentPage = 1;
    await this.loadEmails();
  }

  // Email Detail Modal Methods
  openEmailDetail(emailId: string): void {
    this.selectedEmailId = emailId;
    this.showEmailDetailModal = true;
    this.cdr.markForCheck();
  }

  closeEmailDetailModal(): void {
    this.showEmailDetailModal = false;
    this.selectedEmailId = null;
    this.cdr.markForCheck();
  }
}

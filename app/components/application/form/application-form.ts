import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ApplicationService } from '../../../services/application';
import { AuthService } from '../../../services/auth';
import { SecureStorageService } from '../../../services/secure-storage.service';
import { ToastrService } from 'ngx-toastr';
import { switchMap, tap, debounceTime, distinctUntilChanged, catchError } from 'rxjs/operators';
import { of, Subject, firstValueFrom } from 'rxjs';
import { APPLICATION_LABELS, APPLICATION_MESSAGE_FORMATTERS, APPLICATION_MESSAGES, MESSAGE_TITLES } from '../../../shared/constants/messages';

@Component({
  selector: 'app-application-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NgSelectModule],
  templateUrl: './application-form.html',
  styleUrl: './application-form.scss'
})
export class ApplicationFormComponent implements OnInit {
  private appService = inject(ApplicationService);
  private authService = inject(AuthService);
  private secureStorage = inject(SecureStorageService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);
  private cdr = inject(ChangeDetectorRef);
  private photoCache = new Map<string, string>();

  appData: any = {
    id: 0,
    appName: '',
    description: '',
    appOwner: '',
    ownerEmail: '',
    coOwner: '',
    coOwnerEmail: '',
    userId: '',
    fromEmailAddress: '',
    fromEmailDisplayName: '',
    emailServiceId: null,
    emailServer: '',
    port: null,
    active: 0,
    isInternalApp: false,
    createdBy: '',
    creationDateTime: new Date().toISOString(),
    modifiedBy: '',
    modificationDateTime: new Date().toISOString()
  };

  isEdit: boolean = false;
  users: any[] = [];
  emailServices: any[] = [];
  smtpProvider: string = 'TPInternal';
  smtpProviders: any[] = APPLICATION_LABELS.SMTP_PROVIDERS;
  availableFromEmailDomains: string[] = [];
  fromEmailLocalPart: string = 'support';
  fromEmailDomain: string = '';
  error: string = '';
  ownerUpn: string = '';

  adUsers: any[] = [];
  ownerSearchInput$ = new Subject<string>();
  adUserLoading: boolean = false;
  selectedAdUser: any = null;

  coOwnerAdUsers: any[] = [];
  coOwnerSearchInput$ = new Subject<string>();
  coOwnerLoading: boolean = false;
  selectedCoOwner: any = null;

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('Id');
    const source = this.route.snapshot.queryParams['source'];
    const isFromTPAssist = source === 'tpassist';
    
    if (id) {
      this.isEdit = true;
      await this.loadApplication(+id);
    } else {
      this.appData.createdBy = await this.authService.getUserId();
      this.appData.isInternalApp = true;
      this.appData.emailServiceId = 0;
      if (isFromTPAssist) {
        this.autoPopulateForTPAssist();
      }
    }
    
    await Promise.all([this.loadUsers(), this.loadEmailServices()]);
    this.setupAdUserSearch();
    
    if (isFromTPAssist && !this.isEdit) {
      this.preselectForTPAssist();
    }
  }

  /**
   * Auto-populate owner fields with current logged-in user's information (from TP Assist)
   */
  private autoPopulateForTPAssist(): void {
    const userInfoStr = sessionStorage.getItem('userInfo');
    if (userInfoStr) {
      try {
        const userInfo = JSON.parse(userInfoStr);
        const displayName = userInfo.displayName || userInfo.username || '';
        // Use email, NOT userPrincipalName (which has #EXT# format)
        const email = userInfo.email || '';
        this.ownerUpn = userInfo.userPrincipalName || '';
        
        // Set owner name and email from current user
        this.appData.appOwner = displayName;
        this.appData.ownerEmail = email;
        
        // Create a synthetic AD user object for the dropdown
        if (displayName || email) {
          this.selectedAdUser = {
            displayName: displayName,
            mail: email,
            userPrincipalName: userInfo.userPrincipalName || email
          };
          this.adUsers = [this.selectedAdUser];
          this.loadUserPhoto(this.selectedAdUser, 'owner');
        }
        this.cdr.markForCheck();
      } catch (e) {
        // Silently handle error
      }
    }
  }
  
  /**
   * Pre-select App User and TP Internal for TP Assist navigation
   */
  private async preselectForTPAssist(): Promise<void> {
    // Pre-select current user in App User dropdown
    const currentUserId = await this.authService.getUserId();
    if (currentUserId && this.users.length > 0) {
      this.appData.userId = currentUserId;
    }
    
    // Pre-select TP Internal as the provider
    this.smtpProvider = 'TPInternal';
    this.onSmtpProviderChange();
    
    this.cdr.markForCheck();
  }

  setupAdUserSearch(): void {
    // Owner search typeahead with debounce
    this.ownerSearchInput$.pipe(
      tap(term => {
        if (term && term.length >= 3) {
          this.adUserLoading = true;
          this.cdr.markForCheck();
        }
      }),
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(term => {
        if (!term || term.length < 3) {
          this.adUserLoading = false;
          this.adUsers = [];
          this.cdr.markForCheck();
          return of({ users: [] });
        }
        return this.appService.searchADUsers(term).pipe(
          catchError(() => of({ users: [] }))
        );
      })
    ).subscribe(response => {
      this.adUserLoading = false;
      if (response && response.users) {
        this.adUsers = response.users
          .filter((u: any) => u.mail)
          .map((u: any) => ({
            ...u,
            photoUrl: u.userPrincipalName ? this.photoCache.get(u.userPrincipalName) || null : null
          }));
      }
      this.cdr.markForCheck();
    });

    // Co-Owner search typeahead with debounce
    this.coOwnerSearchInput$.pipe(
      tap(term => {
        if (term && term.length >= 3) {
          this.coOwnerLoading = true;
          this.cdr.markForCheck();
        }
      }),
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(term => {
        if (!term || term.length < 3) {
          this.coOwnerLoading = false;
          this.coOwnerAdUsers = [];
          this.cdr.markForCheck();
          return of({ users: [] });
        }
        return this.appService.searchADUsers(term).pipe(
          catchError(() => of({ users: [] }))
        );
      })
    ).subscribe(response => {
      this.coOwnerLoading = false;
      if (response && response.users) {
        this.coOwnerAdUsers = response.users
          .filter((u: any) => u.mail)
          .map((u: any) => ({
            ...u,
            photoUrl: u.userPrincipalName ? this.photoCache.get(u.userPrincipalName) || null : null
          }));
      }
      this.cdr.markForCheck();
    });
  }

  loadUserPhoto(user: any, type: 'owner' | 'coOwner'): void {
    const upn = user?.userPrincipalName || user?.mail;
    if (!upn) return;

    const cached = this.photoCache.get(upn);
    if (cached) {
      user.photoUrl = cached;
      this.refreshLists(type, cached, user);
      return;
    }

    this.appService.getADUserPhoto(upn).subscribe(res => {
      const photo = res?.data
        || res?.photoUrl
        || res?.photo
        || (typeof res === 'string' ? res : null);
      if (photo) {
        this.photoCache.set(upn, photo);
        user.photoUrl = photo;
        this.refreshLists(type, photo, user);
        // Ensure the currently selected item gets the photo even if matcher misses
        if (type === 'owner' && this.selectedAdUser) {
          this.selectedAdUser = { ...this.selectedAdUser, photoUrl: photo };
        }
        if (type === 'coOwner' && this.selectedCoOwner) {
          this.selectedCoOwner = { ...this.selectedCoOwner, photoUrl: photo };
        }
        this.cdr.markForCheck();
      }
    });
  }

  private refreshLists(type: 'owner' | 'coOwner', photo: string, user: any) {
    if (type === 'owner') {
      this.adUsers = [...this.adUsers];
      if (this.isSameUser(this.selectedAdUser, user)) {
        this.selectedAdUser = { ...this.selectedAdUser, photoUrl: photo };
      }
    } else {
      this.coOwnerAdUsers = [...this.coOwnerAdUsers];
      if (this.isSameUser(this.selectedCoOwner, user)) {
        this.selectedCoOwner = { ...this.selectedCoOwner, photoUrl: photo };
      }
    }
    this.cdr.markForCheck();
  }

  private isSameUser(a: any, b: any): boolean {
    if (!a || !b) return false;
    const norm = (val: string) => (val || '').trim().toLowerCase();
    return norm(a.userPrincipalName) === norm(b.userPrincipalName) || norm(a.mail) === norm(b.mail);
  }

  private syncAppUserSelection(upn?: string, email?: string): void {
    if (!this.users || this.users.length === 0) return;

    const normalize = (val?: string) => (val || '').trim().toLowerCase();
    const targetUpn = normalize(upn);
    const targetEmail = normalize(email);

    let match: any = null;
    if (targetUpn) {
      match = this.users.find(u => normalize(u.upn) === targetUpn);
    }
    if (!match && targetEmail) {
      match = this.users.find(u => normalize(u.email) === targetEmail);
    }

    if (match) {
      this.appData.userId = match.userId;
      this.cdr.markForCheck();
    }
  }

  // Allow adding custom owner name (manual entry)
  addCustomOwner = (term: string) => {
    return { displayName: term, userPrincipalName: '', mail: '' };
  };

  onAdUserSelect(user: any): void {
    if (user) {
      this.appData.appOwner = user.displayName;
      if (user.mail) this.appData.ownerEmail = user.mail;
      this.selectedAdUser = user;
      this.ownerUpn = user.userPrincipalName || user.mail || '';
      if (!user.photoUrl) {
        this.loadUserPhoto(user, 'owner');
      }
      // Regenerate from email if TPInternal is selected
      if (this.isTPInternal()) {
        this.generateFromEmailAddressForTPInternal();
      }
      this.syncAppUserSelection(this.ownerUpn, this.appData.ownerEmail);
    }
  }

  onAdUserClear(): void {
    this.appData.appOwner = '';
    this.appData.ownerEmail = '';
    this.selectedAdUser = null;
    this.adUsers = [];
    this.ownerUpn = '';
  }

  onCoOwnerSelect(user: any): void {
    if (user) {
      this.appData.coOwner = user.displayName;
      if (user.mail) {
        this.appData.coOwnerEmail = user.mail;
      }
      this.selectedCoOwner = {
        ...user,
        mail: user.mail || this.appData.coOwnerEmail || ''
      };
      if (!user.photoUrl) {
        this.loadUserPhoto(user, 'coOwner');
      }
      this.cdr.markForCheck();
    }
  }

  onCoOwnerClear(): void {
    this.appData.coOwner = '';
    this.appData.coOwnerEmail = '';
    this.selectedCoOwner = null;
    this.coOwnerAdUsers = [];
    this.cdr.markForCheck();
  }

  async loadApplication(id: number): Promise<void> {
    try {
      const response = await firstValueFrom(this.appService.getApplicationById(id));
      if (response && response.resultData && response.resultData.length > 0) {
        const data = response.resultData[0];
        this.appData = data;
        
        // Normalize co-owner fields across possible casing variants
        const rawCoOwner = (data as any).coOwner ?? (data as any).CoOwner ?? (data as any).coowner ?? (data as any).COOWNER;
        const rawCoOwnerEmail = (data as any).coOwnerEmail ?? (data as any).CoOwnerEmail ?? (data as any).coowneremail ?? (data as any).COOWNEREMAIL;

        if (!this.appData.coOwner && rawCoOwner) {
          this.appData.coOwner = rawCoOwner;
        }
        if (!this.appData.coOwnerEmail && rawCoOwnerEmail) {
          this.appData.coOwnerEmail = rawCoOwnerEmail;
        }
        
        this.appData.modifiedBy = await this.authService.getUserId();
        
        // Set smtpProvider based on isInternalApp flag
        this.smtpProvider = this.appData.isInternalApp ? 'TPInternal' : 'External';
        
        // Restore owner selection
        if (this.appData.appOwner) {
          this.selectedAdUser = { 
            displayName: this.appData.appOwner, 
            mail: this.appData.ownerEmail || '',
            userPrincipalName: this.appData.ownerEmail || ''
          };
          // Add to adUsers list so ng-select can find it
          this.adUsers = [this.selectedAdUser];
          this.loadUserPhoto(this.selectedAdUser, 'owner');
        }
        
        // Restore co-owner selection
        if (this.appData.coOwner) {
          this.selectedCoOwner = { 
            displayName: this.appData.coOwner, 
            mail: this.appData.coOwnerEmail || '',
            userPrincipalName: this.appData.coOwnerEmail || ''
          };
          // Add to coOwnerAdUsers list so ng-select can find it
          this.coOwnerAdUsers = [this.selectedCoOwner];
          this.loadUserPhoto(this.selectedCoOwner, 'coOwner');
        }
        
        // Restore from email details for TP Internal apps
        if (this.appData.isInternalApp && this.appData.fromEmailAddress) {
          const [local, domain] = this.appData.fromEmailAddress.split('@');
          this.fromEmailLocalPart = local || 'support';
          this.fromEmailDomain = domain || '';
          this.availableFromEmailDomains = [domain || 'teleperformance.com'];
        }
        
        this.cdr.markForCheck();
      }
    } catch (err: any) {
      this.toastr.error(APPLICATION_MESSAGES.LOAD_ERROR, MESSAGE_TITLES.ERROR);
    }
  }

  async loadUsers(): Promise<void> {
    try {
      const response = await firstValueFrom(this.appService.getUsersDDL());
      if (response && response.resultData) {
        this.users = response.resultData;
        const role = await this.authService.getRole();
        if (role === 'USER') {
          const currentUserId = await this.authService.getUserId();
          this.users = this.users.filter(u => u.userId === currentUserId);
          if (!this.isEdit) {
            this.appData.userId = currentUserId;
          }
        }
        this.syncAppUserSelection(this.ownerUpn, this.appData.ownerEmail);
      }
    } catch (error) {
      // Silently handle error
    }
  }

  async loadEmailServices(): Promise<void> {
    try {
      const response = await firstValueFrom(this.appService.getEmailServiceLookups());
      if (response && response.resultData) {
        this.emailServices = response.resultData;
      }
    } catch (error) {
      // Silently handle error
    }
  }

  async onSubmit(): Promise<void> {
    if (this.isEdit) {
      this.appService.updateApplication(this.appData).subscribe({
        next: async (response: any) => {
          this.toastr.success(
            APPLICATION_MESSAGE_FORMATTERS.updateSuccess(this.appData.appName),
            MESSAGE_TITLES.SUCCESS
          );
          // Navigate to test mail page after update so user can test with app password
          this.router.navigate(['/Admin/Email/List'], { queryParams: { appId: this.appData.id } });
        },
        error: (err: any) => {
          this.error = err.error?.detail || APPLICATION_MESSAGES.UPDATE_ERROR;
          this.toastr.error(this.error, MESSAGE_TITLES.ERROR);
        }
      });
    } else {
      const role = await this.authService.getRole();
      // USER and ADMIN roles can both create active applications (no approval required)
      this.appData.active = 1;

      this.appService.addApplication(this.appData).subscribe({
        next: async (response: any) => {
          if (response?.resultData?.[0]?.id && response?.resultData?.[0]?.appSecret) {
            const appId = response.resultData[0].id;
            const appSecret = response.resultData[0].appSecret;
            this.toastr.success(
              APPLICATION_MESSAGE_FORMATTERS.saveSuccess(this.appData.appName),
              MESSAGE_TITLES.SUCCESS
            );
            
            const storePromises: Promise<void>[] = [];
            if (this.appData.ownerEmail) storePromises.push(this.secureStorage.setItem(`app_${appId}_ownerEmail`, this.appData.ownerEmail, true));
            if (this.appData.coOwnerEmail) storePromises.push(this.secureStorage.setItem(`app_${appId}_coOwnerEmail`, this.appData.coOwnerEmail, true));
            storePromises.push(this.secureStorage.setItem(`app_${appId}_appSecret`, appSecret, true));
            await Promise.all(storePromises);
            
            const currentRole = await this.authService.getRole();
            // Navigate to send test email page with appId
            // The guidance email will be sent AFTER user sends test email, not automatically
            // Both ADMIN and USER roles navigate to test mail page after creating application
            this.router.navigate(['/Admin/Email/List'], { queryParams: { appId: appId } });
          }
        },
        error: (err: any) => {
          this.error = err.error?.detail || APPLICATION_MESSAGES.SAVE_ERROR;
          this.toastr.error(this.error, MESSAGE_TITLES.ERROR);
        }
      });
    }
  }

  shouldShowServer(): boolean {
    if (!this.appData.emailServiceId) return false;
    const selectedService = this.emailServices.find(s => s.id === this.appData.emailServiceId);
    if (!selectedService) return false;
    const serviceType = selectedService.type.toString();
    // Show server for SMTP (1), SMTPS (2), SendGrid (4)
    return serviceType === '1' || serviceType === '0' || serviceType === '4';
  }

  shouldShowPort(): boolean {
    if (!this.appData.emailServiceId) return false;
    const selectedService = this.emailServices.find(s => s.id === this.appData.emailServiceId);
    if (!selectedService) return false;
    const serviceType = selectedService.type.toString();
    // Show port for SMTP (1), SMTPS (0/2)
    return serviceType === '1' || serviceType === '0';
  }

  isTPInternal(): boolean {
    return this.smtpProvider === 'TPInternal';
  }

  onSmtpProviderChange(): void {
    if (this.isTPInternal()) {
      // Set isInternalApp flag to true for TPInternal
      this.appData.isInternalApp = true;
      // Populate from email based on owner's domain
      this.generateFromEmailAddressForTPInternal();
      this.appData.emailServer = '';
      this.appData.port = null;
      // Set emailServiceId to 0 for TP Internal (ID 0 = TP Internal in static lookup)
      this.appData.emailServiceId = 0;
    } else {
      // Set isInternalApp flag to false for External
      this.appData.isInternalApp = false;
    }
  }

  private generateFromEmailAddressForTPInternal(): void {
    const currentEmail = this.appData.fromEmailAddress || '';
    if (currentEmail.includes('@')) {
      const [local, domain] = currentEmail.split('@');
      this.fromEmailLocalPart = local || this.fromEmailLocalPart || 'support';
      this.fromEmailDomain = domain || this.fromEmailDomain || '';
    }

    // Extract domain from owner email (e.g., abcd@zdomain.com → zdomain.com)
    const ownerDomain = this.appData.ownerEmail?.split('@')[1]?.toLowerCase() || '';
    const defaultDomains = ['teleperformance.com', 'teleperformanceusa.com'];
    const domainSuggestions = ownerDomain ? [ownerDomain, ...defaultDomains] : defaultDomains;
    this.availableFromEmailDomains = [...new Set(domainSuggestions)];

    if (!this.fromEmailDomain) {
      this.fromEmailDomain = ownerDomain || defaultDomains[0] || '';
    }

    this.updateFromEmailAddress();
  }

  onFromEmailDomainChange(): void {
    this.updateFromEmailAddress();
  }

  onFromEmailLocalPartChange(): void {
    this.updateFromEmailAddress();
  }

  private updateFromEmailAddress(): void {
    const local = (this.fromEmailLocalPart || '').trim();
    const domain = (this.fromEmailDomain || '').trim();
    if (local && domain) {
      this.appData.fromEmailAddress = `${local}@${domain}`;
    } else if (domain) {
      this.appData.fromEmailAddress = domain.includes('@') ? domain : `support@${domain}`;
    } else {
      this.appData.fromEmailAddress = '';
    }
  }
}

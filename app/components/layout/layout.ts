import { Component, inject, OnInit, OnDestroy, HostListener, ElementRef, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { firstValueFrom, Subscription } from 'rxjs';
import { AuthService } from '../../services/auth';
import { ApplicationService } from '../../services/application';
import { DashboardService } from '../../services/dashboard';
import { TPAssistService, TPAssistSearchResponse, TPAssistSuggestion } from '../../services/tpassist';
import { ToastrService } from 'ngx-toastr';
import { IMAGE_PATHS } from '../../shared/constants/image-paths';
import { MESSAGE_TITLES, SESSION_MESSAGES } from '../../shared/constants/messages';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
  encapsulation: ViewEncapsulation.None
})
export class LayoutComponent implements OnInit, OnDestroy {
  public readonly IMAGE_PATHS = IMAGE_PATHS;
  private authService = inject(AuthService);
  private applicationService = inject(ApplicationService);
  private dashboardService = inject(DashboardService);
  private tpAssistService = inject(TPAssistService);
  private router = inject(Router);
  private toastr = inject(ToastrService);
  private elementRef = inject(ElementRef);
  private sanitizer = inject(DomSanitizer);
  private searchSubscription?: Subscription;

  role: string | null = '';
  email: string | null = '';
  displayName: string | null = '';
  dashboardUrl = '';
  isSidebarCollapsed = false;
  isUserDropdownOpen = false;
  headerProfileImage: SafeUrl | string = IMAGE_PATHS.USER_PROFILE;

  // TP Assist Search
  searchQuery = '';
  isSearchOpen = false;
  isSearching = false;
  searchResponse: TPAssistSearchResponse | null = null;
  defaultSuggestions: TPAssistSuggestion[] = [];

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    // Close user dropdown if clicked outside
    if (!this.elementRef.nativeElement.contains(target)) {
      this.isUserDropdownOpen = false;
    }
    // Close search if clicked outside search area
    if (!target.closest('.tpassist-search')) {
      this.isSearchOpen = false;
    }
  }

  toggleUserDropdown(event: Event) {
    event.stopPropagation();
    this.isUserDropdownOpen = !this.isUserDropdownOpen;
  }

  async ngOnInit(): Promise<void> {
    this.role = await this.authService.getRole();
    this.email = await this.authService.getEmail();
    this.dashboardUrl = '/Dashboard'; // Common dashboard for all roles

    // Subscribe to TP Assist search results
    this.searchSubscription = this.tpAssistService.searchResults$.subscribe(response => {
      this.isSearching = false;
      if (response?.resultCode === 1 && response.resultData?.length > 0) {
        this.searchResponse = response.resultData[0];
      } else {
        this.searchResponse = null;
      }
    });

    // Load default suggestions
    this.loadDefaultSuggestions();

    // Fetch user profile from API (like dashboard does) to get UPN reliably
    // This avoids timing issues with localStorage not being set yet
    try {
      const userId = await this.authService.getUserId();
      if (userId) {
        const profileResponse = await firstValueFrom(this.dashboardService.getUserProfile(userId));
        const userProfile = profileResponse?.resultData?.[0] || profileResponse;
        
        if (userProfile) {
          // Set display name and email from API
          this.email = userProfile.email || this.email;
          this.displayName = userProfile.username || (this.email ? this.email.split('@')[0] : 'User');
          
          // Also check localStorage for Azure AD displayName (richer data)
          const userInfoStr = localStorage.getItem('userInfo');
          if (userInfoStr) {
            try {
              const userInfo = JSON.parse(userInfoStr);
              this.displayName = userInfo.displayName || this.displayName;
              this.email = userInfo.email || this.email;
            } catch (e) {
              // Ignore parse errors
            }
          }
          
          console.log('Layout - displayName:', this.displayName, ', email:', this.email);
          await this.authService.updateLoginEmail(this.email!);

          // Fetch profile photo using UPN from API (reliable source)
          if (userProfile.upn) {
            try {
              const photoResponse = await firstValueFrom(
                this.applicationService.getADUserPhoto(userProfile.upn)
              );
              console.log('Layout - Photo response:', photoResponse);
              if (photoResponse?.success && photoResponse?.data) {
                this.headerProfileImage = this.sanitizer.bypassSecurityTrustUrl(photoResponse.data);
                console.log('Layout - Profile image set from API');
              } else {
                console.warn('Layout - Photo response invalid or missing data');
                this.headerProfileImage = this.IMAGE_PATHS.USER_PROFILE;
              }
            } catch (photoError) {
              console.warn('Layout - Could not fetch profile photo:', photoError);
              this.headerProfileImage = this.IMAGE_PATHS.USER_PROFILE;
            }
          } else {
            console.log('Layout - No UPN in user profile from API');
            this.headerProfileImage = this.IMAGE_PATHS.USER_PROFILE;
          }
        } else {
          this.headerProfileImage = this.IMAGE_PATHS.USER_PROFILE;
        }
      } else {
        this.headerProfileImage = this.IMAGE_PATHS.USER_PROFILE;
      }
    } catch (error) {
      console.error('Layout - Error loading user profile:', error);
      this.headerProfileImage = this.IMAGE_PATHS.USER_PROFILE;
    }
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  // TP Assist Search Methods
  openSearch(): void {
    this.isSearchOpen = true;
    this.searchResponse = null;
    setTimeout(() => {
      const input = this.elementRef.nativeElement.querySelector('.tpassist-input');
      input?.focus();
    }, 100);
  }

  closeSearch(): void {
    this.isSearchOpen = false;
    this.searchQuery = '';
    this.searchResponse = null;
  }

  onSearchInput(): void {
    if (this.searchQuery.trim().length >= 2) {
      this.isSearching = true;
      this.tpAssistService.triggerSearch(this.searchQuery);
    } else {
      this.searchResponse = null;
    }
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && this.searchResponse?.success && this.searchResponse.route) {
      this.navigateTo(this.searchResponse.route);
    }
    if (event.key === 'Escape') {
      this.closeSearch();
    }
  }

  navigateTo(route: string): void {
    this.closeSearch();
    // Handle routes with query params
    const [path, queryString] = route.split('?');
    if (queryString) {
      const params: Record<string, string> = {};
      queryString.split('&').forEach(p => {
        const [key, value] = p.split('=');
        params[key] = value;
      });
      this.router.navigate([path], { queryParams: params });
    } else {
      this.router.navigate([path]);
    }
  }

  /**
   * Handle suggestion click - either navigate or get full answer
   */
  async onSuggestionClick(suggestion: TPAssistSuggestion): Promise<void> {
    if (suggestion.type === 'help' && suggestion.answerId !== undefined) {
      // Get the full answer for this help item
      this.isSearching = true;
      try {
        const response = await firstValueFrom(this.tpAssistService.getAnswer(suggestion.answerId));
        if (response?.resultCode === 1 && response.resultData?.length > 0) {
          this.searchResponse = response.resultData[0];
        }
      } catch {
        // Silently fail
      } finally {
        this.isSearching = false;
      }
    } else if (suggestion.route) {
      this.navigateTo(suggestion.route);
    }
  }

  private async loadDefaultSuggestions(): Promise<void> {
    try {
      const response = await firstValueFrom(this.tpAssistService.getSuggestions());
      if (response?.resultCode === 1 && response.resultData?.length > 0) {
        this.defaultSuggestions = response.resultData[0];
      }
    } catch {
      // Silently fail - suggestions are optional
    }
  }

  async logout(): Promise<void> {
    // Show toast first, then logout (logout will redirect to Azure logout page)
    this.toastr.info(SESSION_MESSAGES.LOGOUT_SUCCESS, MESSAGE_TITLES.SESSION_ENDED);
    await this.authService.logout();
  }

  toggleSidebar() {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
    // No body class manipulation needed - layout uses host class binding
  }
}

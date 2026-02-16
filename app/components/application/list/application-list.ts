import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApplicationService } from '../../../services/application';
import { AuthService } from '../../../services/auth';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { Application } from '../../../models/application.model';
import { APPLICATION_APPROVAL_MESSAGES, MESSAGE_TITLES } from '../../../shared/constants/messages';
import { Subject, firstValueFrom } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

declare var $: any;
declare var Swal: any;

@Component({
  selector: 'app-application-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, PaginationComponent],
  templateUrl: './application-list.html',
  styleUrl: './application-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ApplicationListComponent implements OnInit {
  private appService = inject(ApplicationService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  appList: Application[] = [];
  currentPage: number = 1;
  pageSize: number = 5;
  count: number = 0;
  role: string | null = '';
  userId: string | null = '';
  selectedAppId: number = 0;
  viewMode: 'list' | 'card' = 'list';
  searchTerm: string = '';

  // Debounced search
  private searchSubject = new Subject<string>();

  async ngOnInit(): Promise<void> {
    this.role = await this.authService.getRole();
    this.userId = await this.authService.getUserId();
    this.loadApplications();

    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(() => {
      this.onSearch();
    });
  }

  async onSearch(): Promise<void> {
    this.currentPage = 1;
    await this.loadApplications();
  }

  // Debounced search trigger
  onSearchInput(term: string) {
    this.searchTerm = term;
    this.searchSubject.next(term);
  }

  async loadApplications(): Promise<void> {
    try {
      const currentRole = this.role || (await this.authService.getRole());
      let response: any;
      
      if (currentRole === 'ADMIN') {
        response = await firstValueFrom(this.appService.getApplications(this.currentPage, this.pageSize, this.searchTerm));
      } else {
        response = await firstValueFrom(this.appService.getUserApplications(this.userId!, this.currentPage, this.pageSize, this.searchTerm));
      }

      if (response && (response.data || response.resultData)) {
        // Handle both PaginatedResult (.data) and ApiResponse (.resultData)
        const applications = response.data || (response.resultData && response.resultData.length > 0 ? response.resultData : []);
        this.appList = applications;
        this.count = response.totalRecords || 0;
        this.cdr.markForCheck(); // OnPush: ensure view updates after async operation
      }
    } catch (error) {
      console.error('Error loading applications:', error);
      this.cdr.markForCheck();
    }
  }

  async onPageChange(page: number): Promise<void> {
    this.currentPage = page;
    await this.loadApplications();
  }

  showModal(appId: number) {
    this.selectedAppId = appId;
    $('#confirmationModal').modal('show');
  }

  setViewMode(mode: 'list' | 'card') {
    this.viewMode = mode;
    this.cdr.markForCheck();
  }

  async onClickVerify(): Promise<void> {
    $('#confirmationModal').modal('hide');
    try {
      await firstValueFrom(this.appService.approveApplication(this.selectedAppId));
      await Swal.fire({
        icon: 'success',
        title: MESSAGE_TITLES.UPDATED,
        text: APPLICATION_APPROVAL_MESSAGES.APPLICATION_VERIFIED,
        showConfirmButton: false,
        timer: 2500
      });
      await this.loadApplications();
    } catch (err: any) {
      Swal.fire({
        icon: 'error',
        title: MESSAGE_TITLES.ERROR,
        text: err.error?.detail || APPLICATION_APPROVAL_MESSAGES.APPROVAL_ERROR
      });
    }
  }
}


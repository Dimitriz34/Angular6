import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { Router } from '@angular/router';
import { UserService } from '../../services/user';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../shared/components/pagination/pagination';
import { User, Role } from '../../models/user.model';
import { MESSAGE_TITLES, USER_MESSAGES } from '../../shared/constants/messages';
import { Subject, firstValueFrom } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

declare var Swal: any;
declare var bootstrap: any;

@Component({
  selector: 'app-user-details',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent, TitleCasePipe],
  templateUrl: './user-details.html',
  styleUrl: './user-details.scss'
})
export class UserDetailsComponent implements OnInit {
  private userService = inject(UserService);
  private router = inject(Router);

  appUsers: User[] = [];
  roles: Role[] = [];
  currentPage: number = 1;
  pageSize: number = 10;
  count: number = 0;
  selectedUserId: string = '';
  selectedUserForRole: User | null = null;
  selectedNewRoleId: number = 0;
  showUserPanel: boolean = false;
  searchTerm: string = '';
  viewMode: 'list' | 'card' = 'list';
  
  // Filter variables
  filterRole: string = '';
  filterStatus: string = '';
  sortBy: string = '';

  // Debounced search
  private searchSubject = new Subject<string>();

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadUsers(), this.loadRoles()]);

    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(() => {
      this.onSearch();
    });
  }

  async loadUsers(): Promise<void> {
    try {
      const response: any = await firstValueFrom(this.userService.getUsers(
        this.currentPage, 
        this.pageSize, 
        this.searchTerm,
        this.filterRole ? parseInt(this.filterRole, 10) : undefined,
        this.filterStatus !== '' ? parseInt(this.filterStatus, 10) : undefined,
        this.sortBy
      ));
      if (response && (response.data || response.resultData)) {
        const users = response.data || (response.resultData && response.resultData.length > 0 ? response.resultData : []);
        this.appUsers = users;
        this.count = response.totalRecords || 0;
      }
    } catch (error) {
      console.error('Error loading users:', error);
    }
  }

  async loadRoles(): Promise<void> {
    try {
      const response = await firstValueFrom(this.userService.getRoles());
      if (response && response.resultData) {
        // Map API response (id, name) to expected format (roleId, roleName)
        this.roles = response.resultData.map((r: any) => ({
          ...r,
          roleId: String(r.id),
          roleName: r.name
        }));
      }
    } catch (error) {
      console.error('Error loading roles:', error);
    }
  }

  async onSearch(): Promise<void> {
    this.currentPage = 1;
    await this.loadUsers();
  }

  // Debounced search trigger
  onSearchInput(term: string) {
    this.searchTerm = term;
    this.searchSubject.next(term);
  }

  async applyFilters(): Promise<void> {
    this.currentPage = 1;
    await this.loadUsers();
  }

  async clearFilters(): Promise<void> {
    this.filterRole = '';
    this.filterStatus = '';
    this.sortBy = '';
    this.searchTerm = '';
    this.currentPage = 1;
    await this.loadUsers();
  }

  async onPageChange(page: number): Promise<void> {
    this.currentPage = page;
    await this.loadUsers();
  }

  // Select user and open panel
  selectUser(user: User) {
    this.selectedUserForRole = user;
    this.selectedUserId = user.userId;
    this.selectedNewRoleId = user.roles && user.roles.length > 0 ? parseInt(user.roles[0].roleId, 10) : 2;
    this.showUserPanel = true;
  }

  // Navigate to Add User page
  openAddUserPage() {
    this.router.navigate(['/User/AddUser']);
  }

  // Close the panel
  closePanel() {
    this.showUserPanel = false;
    this.selectedUserForRole = null;
  }

  showModal(userId: string) {
    this.selectedUserId = userId;
    const modalEl = document.getElementById('confirmationModal');
    if (modalEl) {
      const modal = new bootstrap.Modal(modalEl);
      modal.show();
    }
  }

  async onClickVerify(): Promise<void> {
    // Close modal if open
    const modalEl = document.getElementById('confirmationModal');
    if (modalEl) {
      bootstrap.Modal.getInstance(modalEl)?.hide();
    }

    const userIdToVerify = this.selectedUserForRole?.userId || this.selectedUserId;
    
    try {
      await firstValueFrom(this.userService.approveUser(userIdToVerify));
      await Swal.fire({
        icon: 'success',
        title: MESSAGE_TITLES.UPDATED,
        text: USER_MESSAGES.ID_VERIFIED,
        showConfirmButton: false,
        timer: 2500
      });
      await this.loadUsers();
      // Update the panel if it's still open
      if (this.selectedUserForRole) {
        this.selectedUserForRole.active = 1;
      }
    } catch (error) {
      console.error('Error verifying user:', error);
    }
  }

  async onClickUpdateRole(): Promise<void> {
    if (!this.selectedUserForRole) return;
    
    try {
      await firstValueFrom(this.userService.updateUserRole(this.selectedUserForRole.userId, this.selectedNewRoleId));
      const newRoleName = this.roles.find(r => r.id === this.selectedNewRoleId)?.name || 'Unknown';
      await Swal.fire({
        icon: 'success',
        title: MESSAGE_TITLES.UPDATED,
        text: `User role updated to ${newRoleName}`,
        showConfirmButton: false,
        timer: 2500
      });
      await this.loadUsers();
      this.closePanel();
    } catch (err) {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'Failed to update user role',
        showConfirmButton: true
      });
    }
  }

  getRoleName(roleId: number): string {
    const role = this.roles.find(r => r.roleId === roleId.toString());
    return role?.roleName || 'Unknown';
  }
}

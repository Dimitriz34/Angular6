import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { LayoutComponent } from './components/layout/layout';
import { AdminDashboardComponent } from './components/admin-dashboard/admin-dashboard';
import { UserDashboardComponent } from './components/user-dashboard/user-dashboard';
import { UserDetailsComponent } from './components/user-details/user-details';
import { PasswordUpdateComponent } from './components/password-update/password-update';
import { ApplicationListComponent } from './components/application/list/application-list';
import { ApplicationFormComponent } from './components/application/form/application-form';
import { AdminRegistrationComponent } from './components/admin-registration/admin-registration';
import { EmailListComponent } from './components/email/list/email-list';
import { EmailLookupListComponent } from './components/email-lookup/list/email-lookup-list';
import { EmailLookupFormComponent } from './components/email-lookup/form/email-lookup-form';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { NoAccessComponent } from './components/no-access/no-access';

export const routes: Routes = [
  { path: '', redirectTo: 'Account/Login', pathMatch: 'full' },
  { path: 'Account/Login', component: LoginComponent },
  { path: 'Account/Registration', component: RegisterComponent },
  { path: 'NoAccess', component: NoAccessComponent },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { 
        path: 'Dashboard', 
        component: UserDashboardComponent
      },
      // Legacy routes for backward compatibility
      { 
        path: 'Admin/Dashboard', 
        component: UserDashboardComponent
      },
      { 
        path: 'User/Dashboard', 
        component: UserDashboardComponent
      },
      { 
        path: 'User/UserDetails', 
        component: UserDetailsComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN'] }
      },
      { 
        path: 'User/AddUser', 
        component: AdminRegistrationComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN'] }
      },
      { path: 'Account/PasswordUpdate', component: PasswordUpdateComponent },
      // Application routes - accessible to both roles
      { 
        path: 'Application/List', 
        component: ApplicationListComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'Application/Add', 
        component: ApplicationFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'Application/Edit/:Id', 
        component: ApplicationFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      // Legacy Admin routes for backward compatibility
      { 
        path: 'Admin/Application/List', 
        component: ApplicationListComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'Admin/Application/Add', 
        component: ApplicationFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'Admin/Application/Edit/:Id', 
        component: ApplicationFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      // User-prefixed routes for backward compatibility
      { 
        path: 'User/Application/List', 
        component: ApplicationListComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'User/Application/Add', 
        component: ApplicationFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'User/Application/Edit/:Id', 
        component: ApplicationFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'Admin/User/Registration', 
        component: AdminRegistrationComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN'] }
      },
      // Email routes - accessible to both roles
      { 
        path: 'Email/List', 
        component: EmailListComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'Admin/Email/List', 
        component: EmailListComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'User/Email/List', 
        component: EmailListComponent,
        canActivate: [roleGuard],
        data: { roles: ['USER', 'ADMIN'] }
      },
      { 
        path: 'Admin/Lookup/EmailServices/List', 
        component: EmailLookupListComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN'] }
      },
      { 
        path: 'Admin/Lookup/EmailServices/Add', 
        component: EmailLookupFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN'] }
      },
      { 
        path: 'Admin/Lookup/EmailServices/Edit/:Id', 
        component: EmailLookupFormComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN'] }
      },
      // Add other routes here as they are migrated
    ]
  },
  { path: '**', redirectTo: 'Account/Login' }
];

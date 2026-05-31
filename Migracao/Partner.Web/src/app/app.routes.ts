import { Routes } from '@angular/router';
import { authGuard, loginRedirectGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { AUTH_PERMISSIONS } from './core/auth/auth-permissions';
import { ShellComponent } from './layout/shell/shell.component';
import { HomeComponent } from './features/home/home.component';
import { LoginComponent } from './features/auth/login/login.component';
import { UsersPageComponent } from './features/users/users.page';
import { MfaSetupComponent } from './features/auth/mfa/mfa-setup.component';
import { MfaChallengeComponent } from './features/auth/mfa/mfa-challenge.component';
import { CompaniesPageComponent } from './features/companies/companies.page';
import { ClientsPageComponent } from './features/clients/clients.page';
import { ImportPageComponent } from './features/import/import.page';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [loginRedirectGuard],
    component: LoginComponent
  },
  {
    path: 'mfa/setup',
    component: MfaSetupComponent
  },
  {
    path: 'mfa',
    component: MfaChallengeComponent
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        component: HomeComponent
      },
      {
        path: 'users',
        canActivate: [permissionGuard(AUTH_PERMISSIONS.usersView)],
        component: UsersPageComponent
      },
      {
        path: 'companies',
        canActivate: [permissionGuard(AUTH_PERMISSIONS.companiesView)],
        component: CompaniesPageComponent
      },
      {
        path: 'clients',
        canActivate: [permissionGuard(AUTH_PERMISSIONS.clientsView)],
        component: ClientsPageComponent
      },
      {
        path: 'import',
        canActivate: [permissionGuard(AUTH_PERMISSIONS.import)],
        component: ImportPageComponent
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];

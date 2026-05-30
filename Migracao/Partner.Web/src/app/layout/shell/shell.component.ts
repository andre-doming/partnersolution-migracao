import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';
import { AUTH_PERMISSIONS } from '../../core/auth/auth-permissions';
import { ThemeService } from '../../core/theme/theme.service';

interface MenuItem {
  label: string;
  link: string;
  permission?: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly themeService = inject(ThemeService);

  private readonly allMenuItems: MenuItem[] = [
    { label: 'Home', link: '/' },
    { label: 'Importar', link: '/import', permission: AUTH_PERMISSIONS.import },
    { label: 'Clientes', link: '/clients', permission: AUTH_PERMISSIONS.clientsView },
    { label: 'Empresas', link: '/companies', permission: AUTH_PERMISSIONS.companiesView },
    { label: 'Usuarios', link: '/users', permission: AUTH_PERMISSIONS.usersView }
  ];

  constructor() {
    this.themeService.initTheme();
  }

  get userName(): string {
    return this.authService.getUserName();
  }

  get menuItems(): MenuItem[] {
    return this.allMenuItems.filter((item) => !item.permission || this.authService.hasPermission(item.permission));
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}

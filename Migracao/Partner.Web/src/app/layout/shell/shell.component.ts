import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { AUTH_PERMISSIONS } from '../../core/auth/auth-permissions';
import { ThemeService } from '../../core/theme/theme.service';
import { ImportService } from '../../features/import/import.service';
import { ImportNotification } from '../../features/import/import.models';
import { NotificationsPanelComponent } from './notifications-panel/notifications-panel.component';

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
    MatIconModule,
    MatBadgeModule,
    MatMenuModule,
    MatSnackBarModule,
    NotificationsPanelComponent
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly themeService = inject(ThemeService);
  private readonly importService = inject(ImportService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  notifications: ImportNotification[] = [];
  unreadCount = 0;
  private lastSeenNotificationId: number | null = null;

  private readonly allMenuItems: MenuItem[] = [
    { label: 'Home', link: '/' },
    { label: 'Importações', link: '/import', permission: AUTH_PERMISSIONS.import },
    { label: 'Clientes', link: '/clients', permission: AUTH_PERMISSIONS.clientsView },
    { label: 'Empresas', link: '/companies', permission: AUTH_PERMISSIONS.companiesView },
    { label: 'Usuarios', link: '/users', permission: AUTH_PERMISSIONS.usersView }
  ];

  constructor() {
    this.themeService.initTheme();
  }

  ngOnInit(): void {
    this.loadNotifications();
    timer(0, 30000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadNotifications(true));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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

  markNotificationRead(notification: ImportNotification): void {
    if (!this.isUnread(notification)) {
      return;
    }

    this.importService.markNotificationRead(notification.id).subscribe({
      next: () => this.refreshNotificationState(notification.id)
    });
  }

  markAllNotificationsRead(): void {
    if (this.unreadCount === 0) {
      return;
    }

    this.importService.markAllNotificationsRead().subscribe({
      next: () => this.refreshNotificationState()
    });
  }

  openNotification(notification: ImportNotification): void {
    this.markNotificationRead(notification);
    this.router.navigate(['/import'], { queryParams: { job: notification.importJobPublicId } });
  }

  isUnread(notification: ImportNotification): boolean {
    return notification.status?.toLowerCase() === 'unread';
  }

  private loadNotifications(isPolling = false): void {
    this.importService.getNotifications().subscribe({
      next: (items) => {
        const normalized = [...items].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc));
        const previousUnreadIds = new Set(this.notifications.filter((item) => this.isUnread(item)).map((item) => item.id));

        this.notifications = normalized;
        this.unreadCount = normalized.filter((item) => this.isUnread(item)).length;

        if (!isPolling) {
          this.lastSeenNotificationId = normalized[0]?.id ?? null;
          return;
        }

        const newItems = normalized.filter((item) => this.isUnread(item) && !previousUnreadIds.has(item.id));
        if (newItems.length > 0) {
          this.lastSeenNotificationId = newItems[0].id;
          const latest = newItems[0];
          this.snackBar.open(`${latest.title}\n${latest.message}`, 'Visualizar', {
            duration: 5000
          }).onAction().subscribe(() => this.openNotification(latest));
        }
      }
    });
  }

  private refreshNotificationState(lastReadId?: number): void {
    this.notifications = this.notifications.map((notification) => {
      if (!this.isUnread(notification)) {
        return notification;
      }

      if (!lastReadId || notification.id === lastReadId) {
        return { ...notification, status: 'Read' };
      }

      return notification;
    });

    if (!lastReadId) {
      this.notifications = this.notifications.map((notification) => ({
        ...notification,
        status: 'Read'
      }));
    }

    this.unreadCount = this.notifications.filter((item) => this.isUnread(item)).length;
  }
}

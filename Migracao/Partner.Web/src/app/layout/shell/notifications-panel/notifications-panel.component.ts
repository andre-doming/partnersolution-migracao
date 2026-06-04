import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ImportNotification } from '../../../features/import/import.models';

@Component({
  selector: 'app-notifications-panel',
  standalone: true,
  imports: [
    CommonModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatTooltipModule
  ],
  templateUrl: './notifications-panel.component.html',
  styleUrl: './notifications-panel.component.scss'
})
export class NotificationsPanelComponent {
  @Input({ required: true }) notifications: ImportNotification[] = [];
  @Input({ required: true }) unreadCount = 0;
  @Output() markAllRead = new EventEmitter<void>();
  @Output() markRead = new EventEmitter<ImportNotification>();
  @Output() openJob = new EventEmitter<ImportNotification>();

  trackById(_: number, item: ImportNotification): number {
    return item.id;
  }

  isUnread(notification: ImportNotification): boolean {
    return notification.status?.toLowerCase() === 'unread';
  }
}

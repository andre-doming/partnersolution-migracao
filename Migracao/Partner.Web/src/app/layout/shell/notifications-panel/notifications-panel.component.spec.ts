/// <reference types="jasmine" />
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationsPanelComponent } from './notifications-panel.component';

describe('NotificationsPanelComponent', () => {
  let component: NotificationsPanelComponent;
  let fixture: ComponentFixture<NotificationsPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationsPanelComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationsPanelComponent);
    component = fixture.componentInstance;
    component.notifications = [];
    component.unreadCount = 0;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should identify unread notifications', () => {
    expect(component.isUnread({
      id: 1,
      title: 'Importação concluída',
      message: 'Mensagem',
      status: 'Unread',
      createdAtUtc: new Date().toISOString(),
      importJobPublicId: '00000000-0000-0000-0000-000000000000'
    })).toBeTrue();
  });
});

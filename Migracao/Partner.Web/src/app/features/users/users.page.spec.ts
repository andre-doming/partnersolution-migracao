import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsersPageComponent } from './users.page';
import { UsersService } from './users.service';
import { AuthService } from '../../core/auth/auth.service';

describe('UsersPageComponent', () => {
  let fixture: ComponentFixture<UsersPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UsersPageComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: UsersService,
          useValue: {
            getLookups: () => of({ companies: [], functions: [] }),
            getList: () => of({ page: 1, pageSize: 10, total: 0, items: [] })
          }
        },
        {
          provide: AuthService,
          useValue: { hasPermission: () => true }
        },
        { provide: MatDialog, useValue: { open: jasmine.createSpy('open'), closeAll: jasmine.createSpy('closeAll') } },
        { provide: MatSnackBar, useValue: { open: jasmine.createSpy('open') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UsersPageComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });
});

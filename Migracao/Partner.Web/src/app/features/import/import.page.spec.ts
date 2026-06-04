import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ImportPageComponent } from './import.page';
import { ImportService } from './import.service';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

describe('ImportPageComponent', () => {
  let fixture: ComponentFixture<ImportPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImportPageComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: ImportService,
          useValue: {
            getLookups: () => of({ companies: [] }),
            getJobs: () => of({ page: 1, pageSize: 10, total: 0, items: [] })
          }
        },
        { provide: ActivatedRoute, useValue: { queryParams: of({}) } },
        { provide: MatSnackBar, useValue: { open: () => {} } },
        { provide: AuthService, useValue: { hasPermission: () => true } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ImportPageComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });
});

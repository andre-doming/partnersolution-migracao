import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { MfaChallengeComponent } from './mfa-challenge.component';
import { MfaService } from './mfa.service';
import { AuthService } from '../../../core/auth/auth.service';

describe('MfaChallengeComponent', () => {
  let fixture: ComponentFixture<MfaChallengeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MfaChallengeComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: MfaService,
          useValue: {
            verify: () =>
              of({
                status: 'LOGIN_SUCCESS',
                accessToken: 'token',
                expiresAtUtc: new Date().toISOString()
              })
          }
        },
        {
          provide: AuthService,
          useValue: {
            getPendingToken: () => 'pending',
            clearPendingToken: jasmine.createSpy('clearPendingToken'),
            completeLogin: jasmine.createSpy('completeLogin')
          }
        },
        { provide: Router, useValue: { navigateByUrl: jasmine.createSpy('navigateByUrl') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MfaChallengeComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { MfaSetupComponent } from './mfa-setup.component';
import { MfaService } from './mfa.service';

describe('MfaSetupComponent', () => {
  let fixture: ComponentFixture<MfaSetupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MfaSetupComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: MfaService,
          useValue: {
            setup: () =>
              of({
                maskedSecret: '****1234',
                manualEntryKey: 'ABCDEFGH',
                qrCodeUrl: 'data:image/png;base64,abc'
              }),
            activate: () => of({ recoveryCodes: ['AAAA-BBBB-CCCC'] })
          }
        },
        { provide: Router, useValue: { navigateByUrl: jasmine.createSpy('navigateByUrl') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MfaSetupComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });
});

import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MfaService } from './mfa.service';
import { MfaSetupResponse } from './mfa.models';

@Component({
  selector: 'app-mfa-setup',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './mfa-setup.component.html',
  styleUrl: './mfa-setup.component.scss'
})
export class MfaSetupComponent implements OnInit {
  private readonly mfaService = inject(MfaService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);

  isLoading = true;
  isActivating = false;
  setupData: MfaSetupResponse | null = null;
  recoveryCodes: string[] | null = null;

  readonly form = this.formBuilder.group({
    totpCode: ['', [Validators.required, Validators.minLength(6)]]
  });

  ngOnInit(): void {
    this.loadSetup();
  }

  loadSetup(): void {
    this.isLoading = true;
    this.mfaService
      .setup()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.setupData = response;
        },
        error: () => {
          this.snackBar.open('Não foi possível iniciar o cadastro MFA.', 'Fechar', {
            duration: 4000,
            verticalPosition: 'top'
          });
        }
      });
  }

  submitActivation(): void {
    if (this.form.invalid || this.isActivating) {
      this.form.markAllAsTouched();
      return;
    }

    this.isActivating = true;
    const totpCode = this.form.controls.totpCode.value ?? '';

    this.mfaService
      .activate(totpCode.trim())
      .pipe(finalize(() => (this.isActivating = false)))
      .subscribe({
        next: (response) => {
          this.recoveryCodes = response.recoveryCodes;
        },
        error: (error) => {
          const message = this.resolveErrorMessage(error);
          this.snackBar.open(message, 'Fechar', { duration: 4000, verticalPosition: 'top' });
        }
      });
  }

  finish(): void {
    this.router.navigateByUrl('/');
  }

  private resolveErrorMessage(error: any): string {
    if (error?.status === 409) {
      return 'MFA já está configurado para este usuário.';
    }
    if (error?.status === 400) {
      return 'Código inválido. Verifique o app autenticador e tente novamente.';
    }

    return 'Não foi possível validar o código MFA.';
  }
}

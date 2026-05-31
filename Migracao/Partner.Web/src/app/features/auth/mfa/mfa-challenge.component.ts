import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/auth/auth.service';
import { MfaService } from './mfa.service';

@Component({
  selector: 'app-mfa-challenge',
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
  templateUrl: './mfa-challenge.component.html',
  styleUrl: './mfa-challenge.component.scss'
})
export class MfaChallengeComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly authService = inject(AuthService);
  private readonly mfaService = inject(MfaService);

  isLoading = false;

  readonly form = this.formBuilder.group({
    totpCode: [''],
    recoveryCode: ['']
  });

  submit(): void {
    if (this.isLoading) {
      return;
    }

    const totp = this.form.controls.totpCode.value?.trim();
    const recovery = this.form.controls.recoveryCode.value?.trim();

    if ((totp && recovery) || (!totp && !recovery)) {
      this.snackBar.open('Informe o código TOTP ou um recovery code.', 'Fechar', {
        duration: 3000,
        verticalPosition: 'top'
      });
      return;
    }

    const pendingToken = this.authService.getPendingToken();
    if (!pendingToken) {
      this.snackBar.open('Sessão expirada. Faça login novamente.', 'Fechar', {
        duration: 3000,
        verticalPosition: 'top'
      });
      this.router.navigateByUrl('/login');
      return;
    }

    this.isLoading = true;
    this.mfaService
      .verify({
        pendingToken,
        totpCode: totp || undefined,
        recoveryCode: recovery || undefined
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.authService.completeLogin(response);
          this.authService.clearPendingToken();
          this.router.navigateByUrl('/');
        },
        error: (error) => {
          const message = this.resolveErrorMessage(error);
          this.snackBar.open(message, 'Fechar', { duration: 4000, verticalPosition: 'top' });
        }
      });
  }

  backToLogin(): void {
    this.authService.clearPendingToken();
    this.router.navigateByUrl('/login');
  }

  private resolveErrorMessage(error: any): string {
    if (error?.status === 409) {
      return error?.error?.message ?? 'Conta temporariamente bloqueada. Tente novamente mais tarde.';
    }
    if (error?.status === 400) {
      return 'Código inválido. Verifique e tente novamente.';
    }
    if (error?.status === 401) {
      this.authService.clearPendingToken();
      this.router.navigateByUrl('/login');
      return 'Sessão expirada. Faça login novamente.';
    }

    return 'Não foi possível validar o MFA.';
  }
}

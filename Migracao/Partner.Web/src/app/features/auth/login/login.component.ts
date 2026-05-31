import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/auth/auth.service';
import { LoginResponse } from '../mfa/mfa.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  isLoading = false;

  readonly form = this.formBuilder.group({
    login: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  onSubmit(): void {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.authService
      .login({
        login: this.form.controls.login.value ?? '',
        password: this.form.controls.password.value ?? ''
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => this.handleLoginResponse(response),
        error: (error) => {
          const message = this.resolveLoginError(error);
          this.snackBar.open(message, 'Fechar', {
            duration: 3000,
            verticalPosition: 'top'
          });
        }
      });
  }

  private handleLoginResponse(response: LoginResponse): void {
    if (response.status === 'LOGIN_SUCCESS') {
      this.router.navigateByUrl('/');
      return;
    }

    if (response.status === 'MFA_SETUP_REQUIRED') {
      this.router.navigateByUrl('/mfa/setup');
      return;
    }

    if (response.status === 'MFA_REQUIRED') {
      this.router.navigateByUrl('/mfa');
      return;
    }

    this.snackBar.open('Não foi possível completar o login.', 'Fechar', {
      duration: 3000,
      verticalPosition: 'top'
    });
  }

  private resolveLoginError(error: any): string {
    const message = error?.error?.message ?? error?.error?.Message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }

    if (error?.status === 401) {
      return 'Usuário ou senha inválidos.';
    }

    return 'Não foi possível realizar o login.';
  }
}

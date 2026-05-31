import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { AUTH_PERMISSIONS } from '../../core/auth/auth-permissions';
import { UsersService } from './users.service';
import {
  UserCompanyLookupItem,
  UserFunctionLookupItem,
  UserListItem,
  UserUpsertRequest
} from './users.models';
import { UserFormDialogComponent } from './user-form-dialog.component';

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  templateUrl: './users.page.html',
  styleUrl: './users.page.scss'
})
export class UsersPageComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  isLoading = false;
  total = 0;
  pageIndex = 0;
  pageSize = 10;

  readonly activeOptions = [
    { value: null, label: 'Todos' },
    { value: 'S', label: 'Ativos' },
    { value: 'N', label: 'Inativos' }
  ];

  readonly filterFields = [
    { value: 'name', label: 'Nome' },
    { value: 'login', label: 'Login' },
    { value: 'email', label: 'E-mail' }
  ];

  companies: UserCompanyLookupItem[] = [];
  functions: UserFunctionLookupItem[] = [];
  items: UserListItem[] = [];

  readonly displayedColumns = ['name', 'login', 'email', 'mfa', 'mfaConfigured', 'lastMfa', 'blocked', 'active', 'admin', 'actions'];

  readonly filterForm = this.fb.group({
    field: this.fb.nonNullable.control<string>('name'),
    value: this.fb.nonNullable.control<string>(''),
    companyId: this.fb.control<number | null>(null),
    active: this.fb.control<'S' | 'N' | null>(null)
  });

  ngOnInit(): void {
    this.loadLookups();
    this.loadUsers();
  }

  get canInsert(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.usersInsert);
  }

  get canUpdate(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.usersUpdate);
  }

  get canDelete(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.usersDelete);
  }

  get canManageMfa(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.usersMfaAdmin);
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadUsers();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  openCreateDialog(): void {
    this.dialog.closeAll();

    const dialogRef = this.dialog.open(UserFormDialogComponent, {
      width: '960px',
      maxWidth: '96vw',
      data: {
        mode: 'create' as const,
        companies: this.companies,
        functions: this.functions
      }
    });

    dialogRef.afterClosed().subscribe((payload: UserUpsertRequest | undefined) => {
      if (!payload) {
        return;
      }

      this.usersService.create(payload).subscribe({
        next: () => {
          this.snackBar.open('Usuário criado com sucesso.', 'Fechar', { duration: 3000 });
          this.loadUsers();
        },
        error: () => this.snackBar.open('Erro ao criar usuário.', 'Fechar', { duration: 3000 })
      });
    });
  }

  openEditDialog(id: number): void {
    this.usersService.getById(id).subscribe({
      next: (detail) => {
        this.dialog.closeAll();

        const dialogRef = this.dialog.open(UserFormDialogComponent, {
          width: '960px',
          maxWidth: '96vw',
          data: {
            mode: 'edit' as const,
            user: detail,
            companies: this.companies,
            functions: this.functions
          }
        });

        dialogRef.afterClosed().subscribe((payload: UserUpsertRequest | undefined) => {
          if (!payload) {
            return;
          }

          this.usersService.update(id, payload).subscribe({
            next: () => {
              this.snackBar.open('Usuário atualizado com sucesso.', 'Fechar', { duration: 3000 });
              this.loadUsers();
            },
            error: () => this.snackBar.open('Erro ao atualizar usuário.', 'Fechar', { duration: 3000 })
          });
        });
      },
      error: () => this.snackBar.open('Erro ao carregar dados do usuário.', 'Fechar', { duration: 3000 })
    });
  }

  removeUser(id: number): void {
    if (!confirm('Deseja inativar este usuário?')) {
      return;
    }

    this.usersService.remove(id).subscribe({
      next: () => {
        this.snackBar.open('Usuário inativado com sucesso.', 'Fechar', { duration: 3000 });
        this.loadUsers();
      },
      error: () => this.snackBar.open('Erro ao inativar usuário.', 'Fechar', { duration: 3000 })
    });
  }

  resetMfa(item: UserListItem): void {
    if (!this.canManageMfa) {
      return;
    }

    if (!confirm(`Deseja resetar o MFA de ${item.name}?`)) {
      return;
    }

    this.usersService.resetMfa(item.id).subscribe({
      next: () => {
        this.snackBar.open('Reset MFA realizado com sucesso. Usuário deverá configurar MFA novamente no próximo login.', 'Fechar', {
          duration: 5000
        });
        this.loadUsers();
      },
      error: (error) => {
        const message = error?.error?.message ?? 'Erro ao resetar MFA.';
        this.snackBar.open(message, 'Fechar', { duration: 4000 });
      }
    });
  }

  unlockUser(item: UserListItem): void {
    if (!this.canManageMfa) {
      return;
    }

    if (!confirm(`Deseja desbloquear o usuário ${item.name}?`)) {
      return;
    }

    this.usersService.unlockUser(item.id).subscribe({
      next: () => {
        this.snackBar.open('Usuário desbloqueado com sucesso.', 'Fechar', { duration: 4000 });
        this.loadUsers();
      },
      error: (error) => {
        const message = error?.error?.message ?? 'Erro ao desbloquear usuário.';
        this.snackBar.open(message, 'Fechar', { duration: 4000 });
      }
    });
  }

  formatDate(value?: string | null): string {
    if (!value) {
      return '-';
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return '-';
    }

    return parsed.toLocaleString('pt-BR');
  }

  resolveMfaStatus(item: UserListItem): string {
    if (!this.canManageMfa || item.mfaEnabled == null) {
      return '-';
    }

    if (item.mfaResetRequired) {
      return 'Reset requerido';
    }

    return item.mfaEnabled ? 'Ativo' : 'Inativo';
  }

  resolveBlockedStatus(item: UserListItem): string {
    if (!this.canManageMfa) {
      return '-';
    }

    const now = new Date();
    const passwordLock = item.passwordLockoutUntil ? new Date(item.passwordLockoutUntil) : null;
    const mfaLock = item.mfaLockoutUntil ? new Date(item.mfaLockoutUntil) : null;
    const isBlocked = (!!passwordLock && passwordLock > now) || (!!mfaLock && mfaLock > now);

    return isBlocked ? 'Sim' : 'Não';
  }

  private loadLookups(): void {
    this.usersService.getLookups().subscribe({
      next: (response) => {
        this.companies = response.companies;
        this.functions = response.functions;
      },
      error: () => this.snackBar.open('Erro ao carregar empresas/permissões.', 'Fechar', { duration: 3000 })
    });
  }

  private loadUsers(): void {
    this.isLoading = true;

    const filter = this.filterForm.getRawValue();

    this.usersService
      .getList({
        page: this.pageIndex + 1,
        pageSize: this.pageSize,
        field: filter.value ? filter.field : undefined,
        value: filter.value || undefined,
        companyId: filter.companyId ?? undefined,
        active: filter.active
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.items = response.items;
          this.total = response.total;
        },
        error: () => this.snackBar.open('Erro ao carregar usuários.', 'Fechar', { duration: 3000 })
      });
  }
}



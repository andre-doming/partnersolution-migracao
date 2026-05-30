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
import { ClientCompanyLookupItem, ClientListItem, ClientUpsertRequest } from './clients.models';
import { ClientsService } from './clients.service';
import { ClientFormDialogComponent } from './client-form-dialog.component';

@Component({
  selector: 'app-clients-page',
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
  templateUrl: './clients.page.html',
  styleUrl: './clients.page.scss'
})
export class ClientsPageComponent implements OnInit {
  private readonly clientsService = inject(ClientsService);
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
    { value: 'firstName', label: 'Nome' },
    { value: 'lastName', label: 'Sobrenome' },
    { value: 'document', label: 'Documento' },
    { value: 'email', label: 'E-mail' },
    { value: 'companyName', label: 'Empresa' }
  ];

  companies: ClientCompanyLookupItem[] = [];
  items: ClientListItem[] = [];

  readonly displayedColumns = ['name', 'document', 'email', 'company', 'approved', 'active', 'actions'];

  readonly filterForm = this.fb.group({
    field: this.fb.nonNullable.control<string>('firstName'),
    value: this.fb.nonNullable.control<string>(''),
    companyId: this.fb.control<number | null>(null),
    active: this.fb.control<'S' | 'N' | null>(null)
  });

  ngOnInit(): void {
    this.loadLookups();
    this.loadClients();
  }

  get canInsert(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.clientsInsert);
  }

  get canUpdate(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.clientsUpdate);
  }

  get canDelete(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.clientsDelete);
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadClients();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadClients();
  }

  openCreateDialog(): void {
    this.dialog.closeAll();

    const dialogRef = this.dialog.open(ClientFormDialogComponent, {
      width: '900px',
      maxWidth: 'calc(100vw - 32px)',
      maxHeight: '90vh',
      autoFocus: false,
      panelClass: 'client-dialog-panel',
      data: {
        mode: 'create' as const,
        companies: this.companies
      }
    });

    dialogRef.afterClosed().subscribe((payload: ClientUpsertRequest | undefined) => {
      if (!payload) {
        return;
      }

      this.clientsService.create(payload).subscribe({
        next: () => {
          this.snackBar.open('Cliente criado com sucesso.', 'Fechar', { duration: 3000 });
          this.loadClients();
        },
        error: () => this.snackBar.open('Erro ao criar cliente.', 'Fechar', { duration: 3000 })
      });
    });
  }

  openEditDialog(id: number): void {
    this.clientsService.getById(id).subscribe({
      next: (detail) => {
        this.dialog.closeAll();

        const dialogRef = this.dialog.open(ClientFormDialogComponent, {
          width: '900px',
          maxWidth: 'calc(100vw - 32px)',
          maxHeight: '90vh',
          autoFocus: false,
          panelClass: 'client-dialog-panel',
          data: {
            mode: 'edit' as const,
            client: detail,
            companies: this.companies
          }
        });

        dialogRef.afterClosed().subscribe((payload: ClientUpsertRequest | undefined) => {
          if (!payload) {
            return;
          }

          this.clientsService.update(id, payload).subscribe({
            next: () => {
              this.snackBar.open('Cliente atualizado com sucesso.', 'Fechar', { duration: 3000 });
              this.loadClients();
            },
            error: () => this.snackBar.open('Erro ao atualizar cliente.', 'Fechar', { duration: 3000 })
          });
        });
      },
      error: () => this.snackBar.open('Erro ao carregar dados do cliente.', 'Fechar', { duration: 3000 })
    });
  }

  removeClient(id: number): void {
    if (!confirm('Deseja inativar este cliente?')) {
      return;
    }

    this.clientsService.remove(id).subscribe({
      next: () => {
        this.snackBar.open('Cliente inativado com sucesso.', 'Fechar', { duration: 3000 });
        this.loadClients();
      },
      error: () => this.snackBar.open('Erro ao inativar cliente.', 'Fechar', { duration: 3000 })
    });
  }

  private loadLookups(): void {
    this.clientsService.getLookups().subscribe({
      next: (response) => {
        this.companies = response.companies;
      },
      error: () => this.snackBar.open('Erro ao carregar empresas.', 'Fechar', { duration: 3000 })
    });
  }

  private loadClients(): void {
    this.isLoading = true;

    const filter = this.filterForm.getRawValue();

    this.clientsService
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
        error: () => this.snackBar.open('Erro ao carregar clientes.', 'Fechar', { duration: 3000 })
      });
  }
}


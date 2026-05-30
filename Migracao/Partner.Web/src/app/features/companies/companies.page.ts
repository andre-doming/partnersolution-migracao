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
import { CompanyUpsertRequest, CompanyListItem } from './companies.models';
import { CompaniesService } from './companies.service';
import { CompanyFormDialogComponent } from './company-form-dialog.component';

@Component({
  selector: 'app-companies-page',
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
  templateUrl: './companies.page.html',
  styleUrl: './companies.page.scss'
})
export class CompaniesPageComponent implements OnInit {
  private readonly companiesService = inject(CompaniesService);
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
    { value: 'S', label: 'Ativas' },
    { value: 'N', label: 'Inativas' }
  ];

  readonly filterFields = [
    { value: 'tradeName', label: 'Nome fantasia' },
    { value: 'corporateName', label: 'Razão social' },
    { value: 'cnpj', label: 'CNPJ' },
    { value: 'managerName', label: 'Gerente responsável' }
  ];

  items: CompanyListItem[] = [];

  readonly displayedColumns = ['tradeName', 'corporateName', 'cnpj', 'managerName', 'active', 'actions'];

  readonly filterForm = this.fb.group({
    field: this.fb.nonNullable.control<string>('tradeName'),
    value: this.fb.nonNullable.control<string>(''),
    active: this.fb.control<'S' | 'N' | null>(null)
  });

  ngOnInit(): void {
    this.loadCompanies();
  }

  get canInsert(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.companiesInsert);
  }

  get canUpdate(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.companiesUpdate);
  }

  get canDelete(): boolean {
    return this.authService.hasPermission(AUTH_PERMISSIONS.companiesDelete);
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadCompanies();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadCompanies();
  }

  openCreateDialog(): void {
    this.dialog.closeAll();

    const dialogRef = this.dialog.open(CompanyFormDialogComponent, {
      width: '780px',
      maxWidth: '92vw',
      maxHeight: '90vh',
      autoFocus: false,
      panelClass: 'company-dialog-panel',
      data: {
        mode: 'create' as const
      }
    });

    dialogRef.afterClosed().subscribe((payload: CompanyUpsertRequest | undefined) => {
      if (!payload) {
        return;
      }

      this.companiesService.create(payload).subscribe({
        next: () => {
          this.snackBar.open('Empresa criada com sucesso.', 'Fechar', { duration: 3000 });
          this.loadCompanies();
        },
        error: () => this.snackBar.open('Erro ao criar empresa.', 'Fechar', { duration: 3000 })
      });
    });
  }

  openEditDialog(id: number): void {
    this.companiesService.getById(id).subscribe({
      next: (detail) => {
        this.dialog.closeAll();

        const dialogRef = this.dialog.open(CompanyFormDialogComponent, {
          width: '780px',
          maxWidth: '92vw',
          maxHeight: '90vh',
          autoFocus: false,
          panelClass: 'company-dialog-panel',
          data: {
            mode: 'edit' as const,
            company: detail
          }
        });

        dialogRef.afterClosed().subscribe((payload: CompanyUpsertRequest | undefined) => {
          if (!payload) {
            return;
          }

          this.companiesService.update(id, payload).subscribe({
            next: () => {
              this.snackBar.open('Empresa atualizada com sucesso.', 'Fechar', { duration: 3000 });
              this.loadCompanies();
            },
            error: () => this.snackBar.open('Erro ao atualizar empresa.', 'Fechar', { duration: 3000 })
          });
        });
      },
      error: () => this.snackBar.open('Erro ao carregar dados da empresa.', 'Fechar', { duration: 3000 })
    });
  }

  removeCompany(id: number): void {
    if (!confirm('Deseja inativar esta empresa?')) {
      return;
    }

    this.companiesService.remove(id).subscribe({
      next: () => {
        this.snackBar.open('Empresa inativada com sucesso.', 'Fechar', { duration: 3000 });
        this.loadCompanies();
      },
      error: () => this.snackBar.open('Erro ao inativar empresa.', 'Fechar', { duration: 3000 })
    });
  }

  private loadCompanies(): void {
    this.isLoading = true;

    const filter = this.filterForm.getRawValue();

    this.companiesService
      .getList({
        page: this.pageIndex + 1,
        pageSize: this.pageSize,
        field: filter.value ? filter.field : undefined,
        value: filter.value || undefined,
        active: filter.active
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.items = response.items;
          this.total = response.total;
        },
        error: () => this.snackBar.open('Erro ao carregar empresas.', 'Fechar', { duration: 3000 })
      });
  }
}


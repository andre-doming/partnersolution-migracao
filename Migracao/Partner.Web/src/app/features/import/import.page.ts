import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { ImportService } from './import.service';
import {
  ImportClientsCsvResponse,
  ImportCompanyLookupItem,
  ImportJobItem,
  ImportPreviewCsvResponse,
  ImportPreviewLine
} from './import.models';

@Component({
  selector: 'app-import-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSnackBarModule
  ],
  templateUrl: './import.page.html',
  styleUrl: './import.page.scss'
})
export class ImportPageComponent implements OnInit {
  private readonly importService = inject(ImportService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  isUploading = false;
  isLoadingJobs = false;
  showHistory = false;
  selectedFile: File | null = null;
  previewResult: ImportPreviewCsvResponse | null = null;
  selectedLineNumbers = new Set<number>();

  companies: ImportCompanyLookupItem[] = [];
  jobs: ImportJobItem[] = [];
  lastResult: ImportClientsCsvResponse | null = null;

  totalJobs = 0;
  pageIndex = 0;
  pageSize = 10;

  readonly uploadForm = this.fb.group({
    companyId: this.fb.nonNullable.control<number>(0, [Validators.required, Validators.min(1)])
  });

  readonly jobColumns = ['id', 'fileName', 'status', 'totalRows', 'successRows', 'errorRows', 'durationMs', 'startedAt'];
  readonly errorColumns = ['lineNumber', 'action', 'document', 'email', 'message'];
  readonly previewColumns = ['select', 'lineNumber', 'action', 'firstName', 'lastName', 'document', 'email', 'validation'];

  ngOnInit(): void {
    this.loadLookups();
    this.loadJobs();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      this.selectedFile = null;
      return;
    }

    const name = file.name.toLowerCase();
    if (!name.endsWith('.csv')) {
      this.snackBar.open('Selecione um arquivo CSV.', 'Fechar', { duration: 3000 });
      input.value = '';
      this.selectedFile = null;
      return;
    }

    this.selectedFile = file;
    this.previewResult = null;
    this.selectedLineNumbers.clear();
    this.lastResult = null;
  }

  preview(): void {
    if (this.uploadForm.invalid || !this.selectedFile) {
      this.uploadForm.markAllAsTouched();
      this.snackBar.open('Selecione empresa e arquivo CSV.', 'Fechar', { duration: 3000 });
      return;
    }

    const companyId = this.uploadForm.getRawValue().companyId;

    this.isUploading = true;
    this.lastResult = null;
    this.previewResult = null;
    this.selectedLineNumbers.clear();

    this.importService
      .previewClientsCsv(this.selectedFile, companyId)
      .pipe(finalize(() => (this.isUploading = false)))
      .subscribe({
        next: (response) => {
          this.previewResult = response;
          for (const line of response.lines) {
            if (line.isValid) {
              this.selectedLineNumbers.add(line.lineNumber);
            }
          }

          this.snackBar.open('Pré-visualização carregada. Selecione os clientes e processe.', 'Fechar', { duration: 3500 });
        },
        error: () => this.snackBar.open('Erro ao pré-visualizar o CSV.', 'Fechar', { duration: 3500 })
      });
  }

  upload(): void {
    this.preview();
  }

  processSelected(): void {
    if (!this.previewResult) {
      this.snackBar.open('Faça a pré-visualização do CSV antes de processar.', 'Fechar', { duration: 3000 });
      return;
    }

    const selectedLines = this.previewResult.lines.filter(
      (line) => line.isValid && this.selectedLineNumbers.has(line.lineNumber)
    );

    if (selectedLines.length === 0) {
      this.snackBar.open('Selecione ao menos um cliente válido para importar.', 'Fechar', { duration: 3000 });
      return;
    }

    this.isUploading = true;
    this.lastResult = null;

    this.importService
      .processSelectedClients({
        companyId: this.previewResult.companyId,
        fileName: this.previewResult.fileName,
        selectedLines
      })
      .pipe(finalize(() => (this.isUploading = false)))
      .subscribe({
        next: (response) => {
          this.lastResult = response;
          this.snackBar.open('Importação dos selecionados finalizada.', 'Fechar', { duration: 3000 });
          this.loadJobs();
        },
        error: () => this.snackBar.open('Erro ao processar clientes selecionados.', 'Fechar', { duration: 3500 })
      });
  }

  toggleLineSelection(lineNumber: number, checked: boolean): void {
    if (checked) {
      this.selectedLineNumbers.add(lineNumber);
      return;
    }

    this.selectedLineNumbers.delete(lineNumber);
  }

  isLineSelected(lineNumber: number): boolean {
    return this.selectedLineNumbers.has(lineNumber);
  }

  selectAllValid(): void {
    if (!this.previewResult) {
      return;
    }

    for (const line of this.previewResult.lines) {
      if (line.isValid) {
        this.selectedLineNumbers.add(line.lineNumber);
      }
    }
  }

  clearSelection(): void {
    this.selectedLineNumbers.clear();
  }

  toggleHistory(): void {
    this.showHistory = !this.showHistory;
  }

  get selectedValidCount(): number {
    if (!this.previewResult) {
      return 0;
    }

    return this.previewResult.lines.filter((line) => line.isValid && this.selectedLineNumbers.has(line.lineNumber)).length;
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadJobs();
  }

  private loadLookups(): void {
    this.importService.getLookups().subscribe({
      next: (response) => {
        this.companies = response.companies;

        if (this.companies.length > 0 && this.uploadForm.controls.companyId.value <= 0) {
          this.uploadForm.controls.companyId.setValue(this.companies[0].id);
        }
      },
      error: () => this.snackBar.open('Erro ao carregar empresas.', 'Fechar', { duration: 3000 })
    });
  }

  private loadJobs(): void {
    this.isLoadingJobs = true;

    this.importService
      .getJobs({ page: this.pageIndex + 1, pageSize: this.pageSize })
      .pipe(finalize(() => (this.isLoadingJobs = false)))
      .subscribe({
        next: (response) => {
          this.jobs = response.items;
          this.totalJobs = response.total;
        },
        error: () => this.snackBar.open('Erro ao carregar histórico de importações.', 'Fechar', { duration: 3000 })
      });
  }
}


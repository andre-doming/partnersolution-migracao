import { Component, OnInit, OnDestroy, inject } from '@angular/core';
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
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { finalize } from 'rxjs/operators';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ImportService } from './import.service';
import { ActivatedRoute } from '@angular/router';
import {
  ImportClientsCsvResponse,
  ImportCompanyLookupItem,
  ImportJobItem,
  ImportJobDetail,
  ImportJobErrorsResponse,
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
    MatSnackBarModule,
    MatExpansionModule,
    MatChipsModule
  ],
  templateUrl: './import.page.html',
  styleUrl: './import.page.scss'
})
export class ImportPageComponent implements OnInit, OnDestroy {
  private readonly importService = inject(ImportService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly destroy$ = new Subject<void>();

  isUploading = false;
  isLoadingJobs = false;
  isLoadingDetails = false;
  isLoadingErrors = false;
  isPolling = false;
  showHistory = false;
  selectedFile: File | null = null;
  previewResult: ImportPreviewCsvResponse | null = null;
  selectedLineNumbers = new Set<number>();

  companies: ImportCompanyLookupItem[] = [];
  jobs: ImportJobItem[] = [];
  lastResult: ImportClientsCsvResponse | null = null;
  selectedJob: ImportJobDetail | null = null;
  selectedJobErrors: ImportJobErrorsResponse | null = null;

  totalJobs = 0;
  pageIndex = 0;
  pageSize = 10;
  errorPageIndex = 0;
  errorPageSize = 10;

  readonly statusOptions = [
    { value: '', label: 'Todos' },
    { value: 'Queued', label: 'Queued' },
    { value: 'Running', label: 'Running' },
    { value: 'Completed', label: 'Completed' },
    { value: 'CompletedWithErrors', label: 'CompletedWithErrors' },
    { value: 'Failed', label: 'Failed' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];

  readonly uploadForm = this.fb.group({
    companyId: this.fb.nonNullable.control<number>(0, [Validators.required, Validators.min(1)])
  });

  readonly filterForm = this.fb.group({
    status: this.fb.nonNullable.control<string>(''),
    startDateUtc: this.fb.control<string | null>(null),
    endDateUtc: this.fb.control<string | null>(null)
  });

  readonly jobColumns = ['fileName', 'feature', 'status', 'progress', 'processedRows', 'errorRows', 'createdAt'];
  readonly errorColumns = ['lineNumber', 'action', 'document', 'email', 'message'];
  readonly previewColumns = ['select', 'lineNumber', 'action', 'firstName', 'lastName', 'document', 'email', 'validation'];
  readonly errorListColumns = ['lineNumber', 'field', 'action', 'document', 'email', 'message'];

  ngOnInit(): void {
    this.loadLookups();
    this.loadJobs();
    this.startPolling();
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      const jobId = params['job'];
      if (!jobId) {
        return;
      }

      if (this.isValidGuid(jobId)) {
        this.loadJobDetails(jobId);
        this.loadJobErrors(jobId);
        this.showHistory = true;
        return;
      }

      this.snackBar.open('JobId inválido. Selecione uma importação válida no histórico.', 'Fechar', {
        duration: 3500
      });
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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

  clearPreviewSelection(): void {
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

  onErrorPageChange(event: PageEvent): void {
    this.errorPageIndex = event.pageIndex;
    this.errorPageSize = event.pageSize;
    if (this.selectedJob) {
      this.loadJobErrors(this.selectedJob.jobPublicId);
    }
  }

  applyFilters(): void {
    this.pageIndex = 0;
    this.loadJobs();
  }

  clearFilters(): void {
    this.filterForm.reset({ status: '', startDateUtc: null, endDateUtc: null });
    this.pageIndex = 0;
    this.loadJobs();
  }

  selectJob(job: ImportJobItem): void {
    this.selectedJob = null;
    this.selectedJobErrors = null;
    this.errorPageIndex = 0;
    this.errorPageSize = 10;
    if (!this.isValidGuid(job.jobPublicId)) {
      this.snackBar.open('JobId inválido. Selecione outra importação.', 'Fechar', { duration: 3500 });
      return;
    }

    this.loadJobDetails(job.jobPublicId);
    this.loadJobErrors(job.jobPublicId);
  }

  clearSelection(): void {
    this.selectedJob = null;
    this.selectedJobErrors = null;
  }

  downloadErrors(): void {
    if (!this.selectedJob || this.selectedJob.errorRows === 0) {
      this.snackBar.open('Nenhum erro disponível para download.', 'Fechar', { duration: 3000 });
      return;
    }

    this.importService.exportJobErrors(this.selectedJob.jobPublicId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `erros_${this.selectedJob?.fileName}_${this.selectedJob?.jobPublicId}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.snackBar.open('Arquivo de erros baixado com sucesso.', 'Fechar', { duration: 3000 });
      },
      error: () => this.snackBar.open('Erro ao baixar arquivo de erros.', 'Fechar', { duration: 3500 })
    });
  }

  isTerminal(status: string): boolean {
    return ['Completed', 'CompletedWithErrors', 'Failed', 'Cancelled'].includes(status);
  }

  formatDuration(durationMs: number): string {
    if (!durationMs || durationMs <= 0) {
      return '-';
    }

    const totalSeconds = Math.floor(durationMs / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}m ${seconds}s`;
  }

  trackJob = (_: number, job: ImportJobItem) => job.jobPublicId;

  private isValidGuid(guid: string): boolean {
    // Rejeita apenas: vazio, null, undefined, ou GUID vazio
    if (!guid || typeof guid !== 'string') {
      return false;
    }

    const trimmed = guid.trim();

    // Rejeita GUID vazio (todos zeros)
    if (trimmed === '00000000-0000-0000-0000-000000000000') {
      return false;
    }

    // Aceita qualquer coisa que pareça um GUID (com hífens e comprimento correto)
    // Formato esperado: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx (36 caracteres)
    if (trimmed.length === 36 && trimmed.match(/^[0-9a-f-]{36}$/i)) {
      return true;
    }

    return false;
  }

  private startPolling(): void {
    this.isPolling = true;
    timer(0, 30000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        const hasActiveJobs = this.jobs.some((job) => job.status === 'Queued' || job.status === 'Running');
        if (hasActiveJobs) {
          this.loadJobs(true);
        }

        if (this.selectedJob && (this.selectedJob.status === 'Queued' || this.selectedJob.status === 'Running') && this.isValidGuid(this.selectedJob.jobPublicId)) {
          this.loadJobDetails(this.selectedJob.jobPublicId, true);
          this.loadJobErrors(this.selectedJob.jobPublicId, true);
        }
      });
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

  private loadJobs(isPolling = false): void {
    if (!isPolling) {
      this.isLoadingJobs = true;
    }

    const filter = this.filterForm.getRawValue();

    this.importService
      .getJobs({
        page: this.pageIndex + 1,
        pageSize: this.pageSize,
        status: filter.status || null,
        startDateUtc: filter.startDateUtc,
        endDateUtc: filter.endDateUtc
      })
      .pipe(finalize(() => (this.isLoadingJobs = false)))
      .subscribe({
        next: (response) => {
          this.jobs = response.items;
          this.totalJobs = response.total;

          if (this.selectedJob) {
            const updated = response.items.find((job) => job.jobPublicId === this.selectedJob?.jobPublicId);
            if (updated) {
              this.selectedJob = { ...this.selectedJob, ...updated };
            }
          }
        },
        error: () => this.snackBar.open('Erro ao carregar histórico de importações.', 'Fechar', { duration: 3000 })
      });
  }

  private loadJobDetails(jobPublicId: string, isPolling = false): void {
    if (!isPolling) {
      this.isLoadingDetails = true;
    }

    this.importService
      .getJobByPublicId(jobPublicId)
      .pipe(finalize(() => (this.isLoadingDetails = false)))
      .subscribe({
        next: (response) => {
          this.selectedJob = response;
        },
        error: () => {
          this.selectedJob = null;
          this.snackBar.open('JobId inválido ou sem acesso. Selecione outra importação.', 'Fechar', { duration: 3500 });
        }
      });
  }

  private loadJobErrors(jobPublicId: string, isPolling = false): void {
    if (!isPolling) {
      this.isLoadingErrors = true;
    }

    this.importService
      .getJobErrors(jobPublicId, this.errorPageIndex + 1, this.errorPageSize)
      .pipe(finalize(() => (this.isLoadingErrors = false)))
      .subscribe({
        next: (response) => {
          this.selectedJobErrors = response;
        },
        error: () => {
          this.selectedJobErrors = null;
          this.snackBar.open('Não foi possível carregar os erros dessa importação.', 'Fechar', { duration: 3500 });
        }
      });
  }
}








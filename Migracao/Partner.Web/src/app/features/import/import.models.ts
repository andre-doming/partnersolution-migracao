export interface ImportClientsCsvResponse {
  jobId: number;
  status: string;
  fileName: string;
  companyId: number;
  totalRows: number;
  successRows: number;
  errorRows: number;
  durationMs: number;
  errors: ImportRowError[];
}

export interface ImportPreviewLine {
  lineNumber: number;
  action: string;
  firstName: string;
  lastName: string;
  document: string;
  email: string;
  gender: string;
  birthDate: string;
  department: string;
  role: string;
  isValid: boolean;
  validationMessage?: string | null;
}

export interface ImportPreviewCsvResponse {
  fileName: string;
  companyId: number;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  lines: ImportPreviewLine[];
}

export interface ImportProcessSelectedRequest {
  companyId: number;
  fileName: string;
  selectedLines: ImportPreviewLine[];
}

export interface ImportRowError {
  lineNumber: number;
  field?: string | null;
  action?: string | null;
  document?: string | null;
  email?: string | null;
  message: string;
}

export interface ImportJobListRequest {
  page: number;
  pageSize: number;
  status?: string | null;
  startDateUtc?: string | null;
  endDateUtc?: string | null;
  userId?: number | null;
}

export interface ImportJobItem {
  id: number;
  jobPublicId: string;
  feature: string;
  fileName: string;
  companyId: number;
  status: string;
  totalRows: number;
  processedRows: number;
  successRows: number;
  errorRows: number;
  progressPercent: number;
  durationMs: number;
  createdAtUtc: string;
  startedAtUtc: string;
  finishedAtUtc?: string | null;
  createdByUserId: number;
}

export interface ImportJobListResponse {
  page: number;
  pageSize: number;
  total: number;
  items: ImportJobItem[];
}

export interface ImportJobDetail extends ImportJobItem {
  errors: ImportRowError[];
}

export interface ImportJobErrorsResponse {
  page: number;
  pageSize: number;
  total: number;
  items: ImportRowError[];
}

export interface ImportCompanyLookupItem {
  id: number;
  name: string;
}

export interface ImportLookupResponse {
  companies: ImportCompanyLookupItem[];
}

export interface ImportNotification {
  id: number;
  title: string;
  message: string;
  status: string;
  createdAtUtc: string;
  importJobPublicId: string;
}





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
  action?: string | null;
  document?: string | null;
  email?: string | null;
  message: string;
}

export interface ImportJobListRequest {
  page: number;
  pageSize: number;
}

export interface ImportJobItem {
  id: number;
  feature: string;
  fileName: string;
  companyId: number;
  status: string;
  totalRows: number;
  successRows: number;
  errorRows: number;
  durationMs: number;
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

export interface ImportCompanyLookupItem {
  id: number;
  name: string;
}

export interface ImportLookupResponse {
  companies: ImportCompanyLookupItem[];
}


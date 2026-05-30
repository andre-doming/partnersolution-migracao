export interface CompanyListRequest {
  page: number;
  pageSize: number;
  field?: string;
  value?: string;
  active?: 'S' | 'N' | null;
}

export interface CompanyListItem {
  id: number;
  cnpj: string;
  tradeName: string;
  corporateName: string;
  managerName: string;
  isActive: boolean;
}

export interface CompanyListResponse {
  page: number;
  pageSize: number;
  total: number;
  items: CompanyListItem[];
}

export interface CompanyDetail {
  id: number;
  cnpj: string;
  tradeName: string;
  corporateName: string;
  managerName: string;
  isActive: boolean;
}

export interface CompanyUpsertRequest {
  cnpj: string;
  tradeName: string;
  corporateName: string;
  managerName: string;
  isActive: boolean;
}


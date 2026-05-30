export interface ClientListRequest {
  page: number;
  pageSize: number;
  field?: string;
  value?: string;
  companyId?: number;
  active?: 'S' | 'N' | null;
}

export interface ClientListItem {
  id: number;
  clientGuid: string;
  firstName: string;
  lastName: string;
  document: string;
  email: string;
  gender?: string | null;
  birthDate?: string | null;
  companyId: number;
  companyName: string;
  department?: string | null;
  role?: string | null;
  approved: boolean;
  isActive: boolean;
}

export interface ClientListResponse {
  page: number;
  pageSize: number;
  total: number;
  items: ClientListItem[];
}

export interface ClientDetail {
  id: number;
  clientGuid: string;
  firstName: string;
  lastName: string;
  document: string;
  email: string;
  gender?: string | null;
  birthDate?: string | null;
  companyId: number;
  companyName: string;
  department?: string | null;
  role?: string | null;
  approved: boolean;
  isActive: boolean;
}

export interface ClientUpsertRequest {
  firstName: string;
  lastName: string;
  document: string;
  email: string;
  gender?: string;
  birthDate?: string;
  companyId: number;
  department?: string;
  role?: string;
  approved: boolean;
  isActive: boolean;
}

export interface ClientCompanyLookupItem {
  id: number;
  name: string;
}

export interface ClientLookupResponse {
  companies: ClientCompanyLookupItem[];
}


export interface UserListRequest {
  page: number;
  pageSize: number;
  field?: string;
  value?: string;
  companyId?: number;
  active?: 'S' | 'N' | null;
}

export interface UserListItem {
  id: number;
  name: string;
  login: string;
  email: string;
  isAdmin: boolean;
  isActive: boolean;
  accessToken: boolean;
  mfaEnabled?: boolean | null;
  mfaConfiguredAt?: string | null;
  lastSuccessfulMfaAt?: string | null;
  mfaResetRequired?: boolean | null;
  failedPasswordAttempts?: number | null;
  passwordLockoutUntil?: string | null;
  failedMfaAttempts?: number | null;
  mfaLockoutUntil?: string | null;
  companyIds: number[];
  permissions: string[];
}

export interface UserListResponse {
  page: number;
  pageSize: number;
  total: number;
  items: UserListItem[];
}

export interface UserDetail {
  id: number;
  name: string;
  login: string;
  email: string;
  isAdmin: boolean;
  isActive: boolean;
  accessToken: boolean;
  firstAccess: boolean;
  companyIds: number[];
  functionIds: number[];
}

export interface UserUpsertRequest {
  name: string;
  login: string;
  password?: string;
  email: string;
  isAdmin: boolean;
  accessToken: boolean;
  isActive: boolean;
  companyIds: number[];
  functionIds: number[];
}

export interface UserCompanyLookupItem {
  id: number;
  name: string;
}

export interface UserFunctionLookupItem {
  id: number;
  code: string;
  name: string;
}

export interface UserLookupResponse {
  companies: UserCompanyLookupItem[];
  functions: UserFunctionLookupItem[];
}



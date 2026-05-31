export type LoginStatus = 'LOGIN_SUCCESS' | 'MFA_REQUIRED' | 'MFA_SETUP_REQUIRED';

export interface LoginResponse {
  status: LoginStatus;
  accessToken?: string | null;
  expiresAtUtc?: string | null;
  name?: string | null;
  pendingToken?: string | null;
  message?: string | null;
}

export interface MfaSetupResponse {
  maskedSecret: string;
  manualEntryKey: string;
  qrCodeUrl: string;
}

export interface MfaActivateResponse {
  recoveryCodes: string[];
}

export interface MfaVerifyResponse {
  status: 'LOGIN_SUCCESS';
  accessToken: string;
  expiresAtUtc: string;
  name?: string | null;
}

export interface MfaVerifyRequest {
  pendingToken: string;
  totpCode?: string | null;
  recoveryCode?: string | null;
}

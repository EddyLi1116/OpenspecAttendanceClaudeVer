import { apiClient } from '../../api/client';

export interface UserSummary {
  id: number;
  email: string;
  displayName: string;
  departmentId: number | null;
  managerUserId: number | null;
  hireDate: string | null;
  employmentStatus: 'active' | 'inactive';
  roleCodes: string[];
}

export interface LoginResponse {
  accessToken: string;
  tokenType: 'Bearer';
  expiresInSeconds: number;
  mustChangePassword: boolean;
  user: UserSummary;
}

export const login = (email: string, password: string) =>
  apiClient.post<LoginResponse>('/auth/login', { email, password }).then((r) => r.data);

export const refresh = () =>
  apiClient.post<LoginResponse>('/auth/refresh').then((r) => r.data);

export const logout = () => apiClient.post<void>('/auth/logout').then((r) => r.data);

export const changePassword = (oldPassword: string, newPassword: string) =>
  apiClient.post<void>('/auth/change-password', { oldPassword, newPassword }).then((r) => r.data);

export const forgotPassword = (email: string) =>
  apiClient.post<void>('/auth/forgot-password', { email }).then((r) => r.data);

export const resetPassword = (token: string, newPassword: string) =>
  apiClient.post<void>('/auth/reset-password', { token, newPassword }).then((r) => r.data);

export const me = () =>
  apiClient.get<UserSummary>('/users/me').then((r) => r.data);

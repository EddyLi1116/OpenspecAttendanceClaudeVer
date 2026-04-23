import { apiClient } from '../../api/client';

export interface UserListItem {
  id: number;
  email: string;
  displayName: string;
  departmentId: number | null;
  departmentName: string | null;
  managerUserId: number | null;
  managerDisplayName: string | null;
  hireDate: string | null;
  employmentStatus: 'active' | 'inactive';
  roleCodes: string[];
}

export interface UserListResponse {
  items: UserListItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface UserListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  departmentId?: number;
  status?: 'active' | 'inactive';
}

export interface CreateUserRequest {
  email: string;
  displayName: string;
  departmentId: number | null;
  managerUserId: number | null;
  hireDate: string | null;
  roleCodes: string[];
}

export interface UpdateUserRequest {
  displayName: string;
  departmentId: number | null;
  managerUserId: number | null;
  hireDate: string | null;
  roleCodes: string[];
}

export const listUsers = (q: UserListQuery = {}) =>
  apiClient.get<UserListResponse>('/users', { params: q }).then((r) => r.data);

export const getUser = (id: number) =>
  apiClient.get<UserListItem>(`/users/${id}`).then((r) => r.data);

export const createUser = (req: CreateUserRequest) =>
  apiClient.post<UserListItem>('/users', req).then((r) => r.data);

export const updateUser = (id: number, req: UpdateUserRequest) =>
  apiClient.put<UserListItem>(`/users/${id}`, req).then((r) => r.data);

export const deactivateUser = (id: number) =>
  apiClient.post<void>(`/users/${id}/deactivate`).then((r) => r.data);

export const activateUser = (id: number) =>
  apiClient.post<void>(`/users/${id}/activate`).then((r) => r.data);

export const resendInvite = (id: number) =>
  apiClient.post<void>(`/users/${id}/resend-invite`).then((r) => r.data);

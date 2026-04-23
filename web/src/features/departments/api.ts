import { apiClient } from '../../api/client';

export interface Department {
  id: number;
  code: string;
  name: string;
  memberCount: number;
}

export interface DepartmentRequest {
  code: string;
  name: string;
}

export const listDepartments = () =>
  apiClient.get<Department[]>('/departments').then((r) => r.data);

export const createDepartment = (req: DepartmentRequest) =>
  apiClient.post<Department>('/departments', req).then((r) => r.data);

export const updateDepartment = (id: number, req: DepartmentRequest) =>
  apiClient.put<Department>(`/departments/${id}`, req).then((r) => r.data);

export const deleteDepartment = (id: number) =>
  apiClient.delete<void>(`/departments/${id}`).then((r) => r.data);

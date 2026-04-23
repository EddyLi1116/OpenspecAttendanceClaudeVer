import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

let accessToken: string | null = null;
export const setAccessToken = (token: string | null) => {
  accessToken = token;
};
export const getAccessToken = () => accessToken;

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// Single in-flight refresh promise to coalesce concurrent 401s.
let refreshPromise: Promise<string | null> | null = null;
let onAuthLost: (() => void) | null = null;
export const setOnAuthLost = (cb: (() => void) | null) => {
  onAuthLost = cb;
};

const callRefresh = async (): Promise<string | null> => {
  try {
    const resp = await axios.post<{ accessToken: string; mustChangePassword: boolean }>(
      `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
      null,
      { withCredentials: true },
    );
    accessToken = resp.data.accessToken;
    return accessToken;
  } catch {
    accessToken = null;
    return null;
  }
};

apiClient.interceptors.response.use(
  (r) => r,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retried?: boolean };
    const status = error.response?.status;
    if (status !== 401 || original._retried) {
      return Promise.reject(error);
    }
    // Don't try to refresh on the auth endpoints themselves.
    if (original.url && /\/auth\/(login|refresh|logout|forgot-password|reset-password)/.test(original.url)) {
      return Promise.reject(error);
    }

    original._retried = true;
    if (!refreshPromise) {
      refreshPromise = callRefresh().finally(() => {
        refreshPromise = null;
      });
    }
    const newToken = await refreshPromise;
    if (!newToken) {
      onAuthLost?.();
      return Promise.reject(error);
    }
    original.headers.Authorization = `Bearer ${newToken}`;
    return apiClient(original);
  },
);

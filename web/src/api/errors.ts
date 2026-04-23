import axios from 'axios';

interface ApiError {
  errorCode?: string;
  message?: string;
  violations?: string[];
}

export const extractErrorMessage = (err: unknown, fallback = '操作失敗'): string => {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as ApiError | undefined;
    if (data?.message) {
      if (data.violations && data.violations.length > 0) {
        return `${data.message}: ${data.violations.join('、')}`;
      }
      return data.message;
    }
    if (err.response?.status === 401) return '帳號或密碼錯誤';
    if (err.response?.status === 403) return '無權限執行此操作';
    return err.message;
  }
  if (err instanceof Error) return err.message;
  return fallback;
};

export const extractErrorCode = (err: unknown): string | undefined => {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as ApiError | undefined;
    return data?.errorCode;
  }
  return undefined;
};

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { setAccessToken, setOnAuthLost } from '../../api/client';
import * as authApi from './api';
import type { LoginResponse, UserSummary } from './api';

interface AuthState {
  user: UserSummary | null;
  mustChangePassword: boolean;
  hydrated: boolean;
  login: (email: string, password: string) => Promise<LoginResponse>;
  logout: () => Promise<void>;
  refresh: () => Promise<void>;
  setMustChangePassword: (v: boolean) => void;
  isInRole: (role: string) => boolean;
}

const AuthContext = createContext<AuthState | null>(null);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<UserSummary | null>(null);
  const [mustChangePassword, setMustChangePassword] = useState(false);
  const [hydrated, setHydrated] = useState(false);

  const applyLogin = useCallback((resp: LoginResponse) => {
    setAccessToken(resp.accessToken);
    setUser(resp.user);
    setMustChangePassword(resp.mustChangePassword);
  }, []);

  const login = useCallback(
    async (email: string, password: string) => {
      const resp = await authApi.login(email, password);
      applyLogin(resp);
      return resp;
    },
    [applyLogin],
  );

  const refresh = useCallback(async () => {
    try {
      const resp = await authApi.refresh();
      applyLogin(resp);
    } catch {
      setAccessToken(null);
      setUser(null);
      setMustChangePassword(false);
    }
  }, [applyLogin]);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      setAccessToken(null);
      setUser(null);
      setMustChangePassword(false);
    }
  }, []);

  useEffect(() => {
    setOnAuthLost(() => {
      setAccessToken(null);
      setUser(null);
      setMustChangePassword(false);
    });
    refresh().finally(() => setHydrated(true));
    return () => setOnAuthLost(null);
  }, [refresh]);

  const value = useMemo<AuthState>(
    () => ({
      user,
      mustChangePassword,
      hydrated,
      login,
      logout,
      refresh,
      setMustChangePassword,
      isInRole: (role) => !!user?.roleCodes.includes(role),
    }),
    [user, mustChangePassword, hydrated, login, logout, refresh],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};

import { Navigate, useLocation } from 'react-router-dom';
import type { ReactElement } from 'react';
import { useAuth } from '../features/auth/AuthContext';

export const RequireAuth = ({ children }: { children: ReactElement }) => {
  const { user, hydrated, mustChangePassword } = useAuth();
  const location = useLocation();
  if (!hydrated) return <div className="p-8 text-gray-500">載入中…</div>;
  if (!user) return <Navigate to="/login" replace state={{ from: location }} />;
  if (mustChangePassword && location.pathname !== '/force-change-password') {
    return <Navigate to="/force-change-password" replace />;
  }
  return children;
};

export const RequireAdmin = ({ children }: { children: ReactElement }) => {
  const { isInRole, hydrated, user } = useAuth();
  if (!hydrated) return <div className="p-8 text-gray-500">載入中…</div>;
  if (!user) return <Navigate to="/login" replace />;
  if (!isInRole('admin')) return <Navigate to="/me" replace />;
  return children;
};

export const RedirectIfAuthed = ({ children }: { children: ReactElement }) => {
  const { user, hydrated } = useAuth();
  if (!hydrated) return null;
  if (user) return <Navigate to="/me" replace />;
  return children;
};

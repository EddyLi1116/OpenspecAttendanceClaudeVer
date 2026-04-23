import { Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from './components/AppLayout';
import { RedirectIfAuthed, RequireAdmin, RequireAuth } from './components/guards';
import LoginPage from './pages/LoginPage';
import ForceChangePasswordPage from './pages/ForceChangePasswordPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import ResetPasswordPage from './pages/ResetPasswordPage';
import ProfilePage from './pages/ProfilePage';
import UsersListPage from './pages/UsersListPage';
import UserFormPage from './pages/UserFormPage';
import DepartmentsPage from './pages/DepartmentsPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<RedirectIfAuthed><LoginPage /></RedirectIfAuthed>} />
      <Route path="/forgot-password" element={<RedirectIfAuthed><ForgotPasswordPage /></RedirectIfAuthed>} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route path="/force-change-password" element={<RequireAuth><ForceChangePasswordPage /></RequireAuth>} />
      <Route element={<RequireAuth><AppLayout /></RequireAuth>}>
        <Route path="/me" element={<ProfilePage />} />
        <Route path="/users" element={<RequireAdmin><UsersListPage /></RequireAdmin>} />
        <Route path="/users/new" element={<RequireAdmin><UserFormPage /></RequireAdmin>} />
        <Route path="/users/:id" element={<RequireAdmin><UserFormPage /></RequireAdmin>} />
        <Route path="/departments" element={<RequireAdmin><DepartmentsPage /></RequireAdmin>} />
        <Route path="/" element={<Navigate to="/me" replace />} />
      </Route>
      <Route path="*" element={<Navigate to="/me" replace />} />
    </Routes>
  );
}

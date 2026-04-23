import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { extractErrorMessage } from '../api/errors';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation() as { state?: { from?: { pathname?: string } } };
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const resp = await login(email, password);
      if (resp.mustChangePassword) {
        navigate('/force-change-password', { replace: true });
      } else {
        const dest = location.state?.from?.pathname && location.state.from.pathname !== '/login'
          ? location.state.from.pathname
          : '/me';
        navigate(dest, { replace: true });
      }
    } catch (err) {
      setError(extractErrorMessage(err, '登入失敗'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <form onSubmit={handleSubmit} className="bg-white p-8 rounded shadow w-full max-w-sm space-y-4">
        <h1 className="text-xl font-semibold text-center">出缺勤系統 登入</h1>
        {error && <div className="bg-red-50 border border-red-200 text-red-700 text-sm p-2 rounded">{error}</div>}
        <label className="block">
          <span className="text-sm text-gray-700">Email</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="username"
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </label>
        <label className="block">
          <span className="text-sm text-gray-700">密碼</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </label>
        <button
          type="submit"
          disabled={loading}
          className="w-full py-2 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60"
        >
          {loading ? '登入中…' : '登入'}
        </button>
        <div className="text-center text-sm">
          <Link to="/forgot-password" className="text-blue-600 hover:underline">忘記密碼?</Link>
        </div>
      </form>
    </div>
  );
}

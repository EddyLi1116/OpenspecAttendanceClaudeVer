import { useMemo, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { resetPassword } from '../features/auth/api';
import { extractErrorMessage } from '../api/errors';
import { evaluatePassword } from '../features/auth/passwordPolicy';

export default function ResetPasswordPage() {
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';
  const navigate = useNavigate();
  const [newPassword, setNewPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const policy = useMemo(() => evaluatePassword(newPassword), [newPassword]);

  if (!token) {
    return (
      <div className="min-h-screen flex items-center justify-center px-4">
        <div className="bg-white p-8 rounded shadow text-sm text-red-700">
          連結無效，請<Link to="/forgot-password" className="text-blue-600 underline">重新申請</Link>。
        </div>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!policy.ok) return setError('新密碼不符合複雜度');
    if (newPassword !== confirm) return setError('兩次新密碼不一致');
    setLoading(true);
    try {
      await resetPassword(token, newPassword);
      navigate('/login', { replace: true });
    } catch (err) {
      setError(extractErrorMessage(err, '重設失敗'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <form onSubmit={handleSubmit} className="bg-white p-8 rounded shadow w-full max-w-sm space-y-4">
        <h1 className="text-xl font-semibold">重設密碼</h1>
        {error && <div className="bg-red-50 border border-red-200 text-red-700 text-sm p-2 rounded">{error}</div>}
        <label className="block">
          <span className="text-sm text-gray-700">新密碼</span>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          />
        </label>
        <label className="block">
          <span className="text-sm text-gray-700">確認新密碼</span>
          <input
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          />
        </label>
        <button type="submit" disabled={loading || !policy.ok} className="w-full py-2 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60">
          {loading ? '送出中…' : '重設密碼'}
        </button>
      </form>
    </div>
  );
}

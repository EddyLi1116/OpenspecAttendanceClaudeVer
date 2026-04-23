import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { changePassword } from '../features/auth/api';
import { extractErrorMessage } from '../api/errors';
import { evaluatePassword } from '../features/auth/passwordPolicy';

export default function ForceChangePasswordPage() {
  const { setMustChangePassword, refresh } = useAuth();
  const navigate = useNavigate();
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const policy = useMemo(() => evaluatePassword(newPassword), [newPassword]);
  const mismatch = newPassword.length > 0 && confirm.length > 0 && newPassword !== confirm;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!policy.ok) {
      setError('新密碼不符合複雜度');
      return;
    }
    if (newPassword !== confirm) {
      setError('兩次新密碼不一致');
      return;
    }
    setLoading(true);
    try {
      await changePassword(oldPassword, newPassword);
      setMustChangePassword(false);
      await refresh();
      navigate('/me', { replace: true });
    } catch (err) {
      setError(extractErrorMessage(err, '密碼變更失敗'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <form onSubmit={handleSubmit} className="bg-white p-8 rounded shadow w-full max-w-md space-y-4">
        <h1 className="text-xl font-semibold">首次登入：請設定新密碼</h1>
        {error && <div className="bg-red-50 border border-red-200 text-red-700 text-sm p-2 rounded">{error}</div>}
        <label className="block">
          <span className="text-sm text-gray-700">舊密碼（信中提供的初始密碼）</span>
          <input
            type="password"
            value={oldPassword}
            onChange={(e) => setOldPassword(e.target.value)}
            required
            autoComplete="current-password"
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          />
        </label>
        <label className="block">
          <span className="text-sm text-gray-700">新密碼</span>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            autoComplete="new-password"
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          />
        </label>
        <ul className="text-xs space-y-0.5">
          <PolicyItem ok={policy.length}>至少 10 個字元</PolicyItem>
          <PolicyItem ok={policy.upper}>包含大寫字母</PolicyItem>
          <PolicyItem ok={policy.lower}>包含小寫字母</PolicyItem>
          <PolicyItem ok={policy.digit}>包含數字</PolicyItem>
          <PolicyItem ok={policy.symbol}>包含符號</PolicyItem>
        </ul>
        <label className="block">
          <span className="text-sm text-gray-700">確認新密碼</span>
          <input
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
            autoComplete="new-password"
            className={`mt-1 w-full border rounded px-3 py-2 ${mismatch ? 'border-red-400' : 'border-gray-300'}`}
          />
          {mismatch && <span className="text-xs text-red-600">兩次密碼不一致</span>}
        </label>
        <button
          type="submit"
          disabled={loading || !policy.ok || mismatch}
          className="w-full py-2 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60"
        >
          {loading ? '送出中…' : '更新密碼'}
        </button>
      </form>
    </div>
  );
}

const PolicyItem = ({ ok, children }: { ok: boolean; children: React.ReactNode }) => (
  <li className={ok ? 'text-green-700' : 'text-gray-500'}>
    {ok ? '✓ ' : '○ '}
    {children}
  </li>
);

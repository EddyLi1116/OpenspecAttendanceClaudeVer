import { useState } from 'react';
import { Link } from 'react-router-dom';
import { forgotPassword } from '../features/auth/api';
import { extractErrorMessage } from '../api/errors';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await forgotPassword(email);
      setDone(true);
    } catch (err) {
      setError(extractErrorMessage(err, '寄送失敗'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <form onSubmit={handleSubmit} className="bg-white p-8 rounded shadow w-full max-w-sm space-y-4">
        <h1 className="text-xl font-semibold">忘記密碼</h1>
        {done ? (
          <div className="text-sm text-green-700">若帳號存在，系統已寄出重設連結，請於 30 分鐘內完成。</div>
        ) : (
          <>
            {error && <div className="bg-red-50 border border-red-200 text-red-700 text-sm p-2 rounded">{error}</div>}
            <label className="block">
              <span className="text-sm text-gray-700">註冊 Email</span>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
              />
            </label>
            <button type="submit" disabled={loading} className="w-full py-2 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60">
              {loading ? '送出中…' : '寄出重設連結'}
            </button>
          </>
        )}
        <div className="text-center text-sm">
          <Link to="/login" className="text-blue-600 hover:underline">返回登入</Link>
        </div>
      </form>
    </div>
  );
}

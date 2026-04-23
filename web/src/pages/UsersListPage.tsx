import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  activateUser,
  deactivateUser,
  listUsers,
  resendInvite,
  type UserListQuery,
} from '../features/users/api';
import { listDepartments } from '../features/departments/api';
import { extractErrorMessage } from '../api/errors';

export default function UsersListPage() {
  const [query, setQuery] = useState<UserListQuery>({ page: 1, pageSize: 20 });
  const [search, setSearch] = useState('');
  const qc = useQueryClient();

  const usersQuery = useQuery({
    queryKey: ['users', query],
    queryFn: () => listUsers(query),
  });
  const deptsQuery = useQuery({ queryKey: ['departments'], queryFn: listDepartments });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['users'] });

  const deactivateMutation = useMutation({ mutationFn: deactivateUser, onSuccess: invalidate });
  const activateMutation = useMutation({ mutationFn: activateUser, onSuccess: invalidate });
  const resendMutation = useMutation({ mutationFn: resendInvite });

  const handleAction = async (action: () => Promise<unknown>, confirmText: string, successText?: string) => {
    if (!window.confirm(confirmText)) return;
    try {
      await action();
      if (successText) window.alert(successText);
    } catch (err) {
      window.alert(extractErrorMessage(err));
    }
  };

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setQuery((q) => ({ ...q, page: 1, search: search || undefined }));
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">使用者管理</h1>
        <Link to="/users/new" className="px-3 py-1.5 rounded bg-blue-600 text-white text-sm hover:bg-blue-700">＋ 新增使用者</Link>
      </div>

      <form onSubmit={submitSearch} className="flex flex-wrap gap-3 items-end bg-white border border-gray-200 rounded p-3">
        <label className="text-sm">
          <span className="block text-gray-600">關鍵字（email/姓名）</span>
          <input value={search} onChange={(e) => setSearch(e.target.value)} className="border border-gray-300 rounded px-2 py-1" />
        </label>
        <label className="text-sm">
          <span className="block text-gray-600">部門</span>
          <select
            value={query.departmentId ?? ''}
            onChange={(e) => setQuery((q) => ({ ...q, page: 1, departmentId: e.target.value ? Number(e.target.value) : undefined }))}
            className="border border-gray-300 rounded px-2 py-1"
          >
            <option value="">全部</option>
            {deptsQuery.data?.map((d) => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        </label>
        <label className="text-sm">
          <span className="block text-gray-600">狀態</span>
          <select
            value={query.status ?? ''}
            onChange={(e) => setQuery((q) => ({ ...q, page: 1, status: (e.target.value || undefined) as UserListQuery['status'] }))}
            className="border border-gray-300 rounded px-2 py-1"
          >
            <option value="">全部</option>
            <option value="active">在職</option>
            <option value="inactive">停用</option>
          </select>
        </label>
        <button type="submit" className="px-3 py-1.5 rounded bg-gray-800 text-white text-sm">查詢</button>
      </form>

      <div className="bg-white border border-gray-200 rounded overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 text-left text-gray-600">
            <tr>
              <th className="px-3 py-2">Email</th>
              <th className="px-3 py-2">姓名</th>
              <th className="px-3 py-2">部門</th>
              <th className="px-3 py-2">主管</th>
              <th className="px-3 py-2">狀態</th>
              <th className="px-3 py-2">操作</th>
            </tr>
          </thead>
          <tbody>
            {usersQuery.isLoading && (
              <tr><td colSpan={6} className="px-3 py-6 text-center text-gray-500">載入中…</td></tr>
            )}
            {usersQuery.data?.items.map((u) => (
              <tr key={u.id} className="border-t border-gray-100">
                <td className="px-3 py-2">
                  <Link to={`/users/${u.id}`} className="text-blue-600 hover:underline">{u.email}</Link>
                </td>
                <td className="px-3 py-2">{u.displayName}</td>
                <td className="px-3 py-2">{u.departmentName ?? '—'}</td>
                <td className="px-3 py-2">{u.managerDisplayName ?? '—'}</td>
                <td className="px-3 py-2">
                  <span className={u.employmentStatus === 'active' ? 'text-green-700' : 'text-gray-500'}>
                    {u.employmentStatus === 'active' ? '在職' : '停用'}
                  </span>
                </td>
                <td className="px-3 py-2 space-x-2">
                  {u.employmentStatus === 'active' ? (
                    <button
                      className="text-red-600 hover:underline"
                      onClick={() => handleAction(() => deactivateMutation.mutateAsync(u.id), `停用 ${u.email}?`)}
                    >停用</button>
                  ) : (
                    <button
                      className="text-green-700 hover:underline"
                      onClick={() => handleAction(() => activateMutation.mutateAsync(u.id), `啟用 ${u.email}?`)}
                    >啟用</button>
                  )}
                  <button
                    className="text-blue-600 hover:underline"
                    onClick={() => handleAction(() => resendMutation.mutateAsync(u.id), `重發邀請信給 ${u.email}?`, '已寄出新邀請信')}
                  >重發邀請</button>
                </td>
              </tr>
            ))}
            {usersQuery.data && usersQuery.data.items.length === 0 && (
              <tr><td colSpan={6} className="px-3 py-6 text-center text-gray-500">沒有資料</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {usersQuery.data && (
        <div className="flex items-center justify-between text-sm">
          <span className="text-gray-500">總計 {usersQuery.data.total} 筆，第 {usersQuery.data.page} / {Math.max(1, Math.ceil(usersQuery.data.total / usersQuery.data.pageSize))} 頁</span>
          <div className="space-x-2">
            <button
              disabled={(query.page ?? 1) <= 1}
              onClick={() => setQuery((q) => ({ ...q, page: Math.max(1, (q.page ?? 1) - 1) }))}
              className="px-3 py-1 border border-gray-300 rounded disabled:opacity-50"
            >上一頁</button>
            <button
              disabled={(query.page ?? 1) * (query.pageSize ?? 20) >= usersQuery.data.total}
              onClick={() => setQuery((q) => ({ ...q, page: (q.page ?? 1) + 1 }))}
              className="px-3 py-1 border border-gray-300 rounded disabled:opacity-50"
            >下一頁</button>
          </div>
        </div>
      )}
    </div>
  );
}

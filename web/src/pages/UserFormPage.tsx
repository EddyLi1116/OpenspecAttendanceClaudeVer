import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { createUser, getUser, listUsers, updateUser } from '../features/users/api';
import { listDepartments } from '../features/departments/api';
import { extractErrorMessage } from '../api/errors';

const ROLES = ['admin', 'employee'] as const;

export default function UserFormPage() {
  const { id } = useParams();
  const isEdit = id !== undefined && id !== 'new';
  const userId = isEdit ? Number(id) : undefined;
  const navigate = useNavigate();

  const deptsQuery = useQuery({ queryKey: ['departments'], queryFn: listDepartments });
  const managersQuery = useQuery({
    queryKey: ['users', { pageSize: 500 }],
    queryFn: () => listUsers({ pageSize: 500, status: 'active' }),
  });
  const userQuery = useQuery({
    queryKey: ['user', userId],
    queryFn: () => getUser(userId!),
    enabled: isEdit,
  });

  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [departmentId, setDepartmentId] = useState<number | ''>('');
  const [managerUserId, setManagerUserId] = useState<number | ''>('');
  const [hireDate, setHireDate] = useState('');
  const [roles, setRoles] = useState<string[]>(['employee']);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (userQuery.data) {
      setEmail(userQuery.data.email);
      setDisplayName(userQuery.data.displayName);
      setDepartmentId(userQuery.data.departmentId ?? '');
      setManagerUserId(userQuery.data.managerUserId ?? '');
      setHireDate(userQuery.data.hireDate ?? '');
      setRoles(userQuery.data.roleCodes);
    }
  }, [userQuery.data]);

  const toggleRole = (r: string) => {
    setRoles((cur) => (cur.includes(r) ? cur.filter((x) => x !== r) : [...cur, r]));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const payload = {
        displayName,
        departmentId: departmentId === '' ? null : Number(departmentId),
        managerUserId: managerUserId === '' ? null : Number(managerUserId),
        hireDate: hireDate || null,
        roleCodes: roles,
      };
      if (isEdit) {
        await updateUser(userId!, payload);
      } else {
        await createUser({ email, ...payload });
      }
      navigate('/users');
    } catch (err) {
      setError(extractErrorMessage(err, '儲存失敗'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="bg-white border border-gray-200 rounded p-6 max-w-2xl">
      <h1 className="text-xl font-semibold mb-4">{isEdit ? '編輯使用者' : '新增使用者'}</h1>
      {error && <div className="bg-red-50 border border-red-200 text-red-700 text-sm p-2 rounded mb-3">{error}</div>}
      <form onSubmit={handleSubmit} className="space-y-3 text-sm">
        <label className="block">
          <span className="text-gray-700">Email</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={isEdit}
            required
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2 disabled:bg-gray-100"
          />
        </label>
        <label className="block">
          <span className="text-gray-700">姓名</span>
          <input
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            required
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          />
        </label>
        <label className="block">
          <span className="text-gray-700">部門</span>
          <select
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value === '' ? '' : Number(e.target.value))}
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          >
            <option value="">— 未指派 —</option>
            {deptsQuery.data?.map((d) => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        </label>
        <label className="block">
          <span className="text-gray-700">直屬主管</span>
          <select
            value={managerUserId}
            onChange={(e) => setManagerUserId(e.target.value === '' ? '' : Number(e.target.value))}
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          >
            <option value="">— 無 —</option>
            {managersQuery.data?.items.filter((u) => u.id !== userId).map((u) => (
              <option key={u.id} value={u.id}>{u.displayName} ({u.email})</option>
            ))}
          </select>
        </label>
        <label className="block">
          <span className="text-gray-700">到職日</span>
          <input
            type="date"
            value={hireDate}
            onChange={(e) => setHireDate(e.target.value)}
            className="mt-1 w-full border border-gray-300 rounded px-3 py-2"
          />
        </label>
        <fieldset>
          <legend className="text-gray-700 mb-1">角色</legend>
          <div className="flex gap-4">
            {ROLES.map((r) => (
              <label key={r} className="flex items-center gap-2">
                <input type="checkbox" checked={roles.includes(r)} onChange={() => toggleRole(r)} />
                <span>{r}</span>
              </label>
            ))}
          </div>
        </fieldset>
        <div className="flex gap-2 pt-3">
          <button type="submit" disabled={submitting} className="px-4 py-2 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60">
            {submitting ? '儲存中…' : '儲存'}
          </button>
          <button type="button" onClick={() => navigate('/users')} className="px-4 py-2 rounded bg-gray-200 hover:bg-gray-300">取消</button>
        </div>
      </form>
    </div>
  );
}

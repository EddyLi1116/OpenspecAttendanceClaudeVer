import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createDepartment,
  deleteDepartment,
  listDepartments,
  updateDepartment,
  type Department,
} from '../features/departments/api';
import { extractErrorMessage } from '../api/errors';

export default function DepartmentsPage() {
  const qc = useQueryClient();
  const query = useQuery({ queryKey: ['departments'], queryFn: listDepartments });
  const [editing, setEditing] = useState<Department | null>(null);
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);

  const reset = () => {
    setEditing(null);
    setCode('');
    setName('');
    setError(null);
  };

  const onSuccess = () => {
    qc.invalidateQueries({ queryKey: ['departments'] });
    reset();
  };

  const createMutation = useMutation({ mutationFn: createDepartment, onSuccess });
  const updateMutation = useMutation({ mutationFn: ({ id, code, name }: { id: number; code: string; name: string }) => updateDepartment(id, { code, name }), onSuccess });
  const deleteMutation = useMutation({ mutationFn: deleteDepartment, onSuccess: () => qc.invalidateQueries({ queryKey: ['departments'] }) });

  const startEdit = (d: Department) => {
    setEditing(d);
    setCode(d.code);
    setName(d.name);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, code, name });
      } else {
        await createMutation.mutateAsync({ code, name });
      }
    } catch (err) {
      setError(extractErrorMessage(err, '儲存失敗'));
    }
  };

  const handleDelete = async (d: Department) => {
    if (!window.confirm(`刪除部門 ${d.name}?`)) return;
    try {
      await deleteMutation.mutateAsync(d.id);
    } catch (err) {
      window.alert(extractErrorMessage(err));
    }
  };

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div className="md:col-span-2 bg-white border border-gray-200 rounded">
        <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
          <h1 className="font-semibold">部門</h1>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-gray-50 text-left text-gray-600">
            <tr>
              <th className="px-3 py-2">代碼</th>
              <th className="px-3 py-2">名稱</th>
              <th className="px-3 py-2">成員數</th>
              <th className="px-3 py-2">操作</th>
            </tr>
          </thead>
          <tbody>
            {query.data?.map((d) => (
              <tr key={d.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{d.code}</td>
                <td className="px-3 py-2">{d.name}</td>
                <td className="px-3 py-2">{d.memberCount}</td>
                <td className="px-3 py-2 space-x-2">
                  <button onClick={() => startEdit(d)} className="text-blue-600 hover:underline">編輯</button>
                  <button onClick={() => handleDelete(d)} className="text-red-600 hover:underline">刪除</button>
                </td>
              </tr>
            ))}
            {query.data && query.data.length === 0 && (
              <tr><td colSpan={4} className="px-3 py-6 text-center text-gray-500">尚無部門</td></tr>
            )}
          </tbody>
        </table>
      </div>
      <form onSubmit={handleSubmit} className="bg-white border border-gray-200 rounded p-4 space-y-3 text-sm h-fit">
        <h2 className="font-semibold">{editing ? `編輯部門：${editing.name}` : '新增部門'}</h2>
        {error && <div className="bg-red-50 border border-red-200 text-red-700 text-xs p-2 rounded">{error}</div>}
        <label className="block">
          <span className="text-gray-700">代碼</span>
          <input value={code} onChange={(e) => setCode(e.target.value)} required className="mt-1 w-full border border-gray-300 rounded px-3 py-2" />
        </label>
        <label className="block">
          <span className="text-gray-700">名稱</span>
          <input value={name} onChange={(e) => setName(e.target.value)} required className="mt-1 w-full border border-gray-300 rounded px-3 py-2" />
        </label>
        <div className="flex gap-2">
          <button type="submit" className="px-3 py-1.5 rounded bg-blue-600 text-white hover:bg-blue-700">{editing ? '更新' : '新增'}</button>
          {editing && <button type="button" onClick={reset} className="px-3 py-1.5 rounded bg-gray-200 hover:bg-gray-300">取消</button>}
        </div>
      </form>
    </div>
  );
}

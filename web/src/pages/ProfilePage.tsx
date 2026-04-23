import { useQuery } from '@tanstack/react-query';
import { me } from '../features/auth/api';

export default function ProfilePage() {
  const { data, isLoading, error } = useQuery({ queryKey: ['me'], queryFn: me });

  if (isLoading) return <div className="text-gray-500">載入中…</div>;
  if (error || !data) return <div className="text-red-700">無法載入個人資料</div>;

  return (
    <div className="bg-white border border-gray-200 rounded p-6 max-w-xl">
      <h1 className="text-xl font-semibold mb-4">我的資料</h1>
      <dl className="grid grid-cols-3 gap-y-3 text-sm">
        <dt className="text-gray-500">Email</dt>
        <dd className="col-span-2">{data.email}</dd>
        <dt className="text-gray-500">姓名</dt>
        <dd className="col-span-2">{data.displayName}</dd>
        <dt className="text-gray-500">在職狀態</dt>
        <dd className="col-span-2">{data.employmentStatus === 'active' ? '在職' : '停用'}</dd>
        <dt className="text-gray-500">角色</dt>
        <dd className="col-span-2">{data.roleCodes.join('、')}</dd>
        <dt className="text-gray-500">到職日</dt>
        <dd className="col-span-2">{data.hireDate ?? '—'}</dd>
      </dl>
    </div>
  );
}

import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';

export default function AppLayout() {
  const { user, logout, isInRole } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="min-h-screen flex flex-col bg-gray-50 text-gray-900">
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-6xl mx-auto px-6 h-14 flex items-center justify-between">
          <Link to="/" className="font-semibold text-lg">出缺勤系統</Link>
          <nav className="flex gap-4 items-center text-sm">
            <NavLink to="/me" className={({ isActive }) => (isActive ? 'font-semibold' : 'text-gray-600 hover:text-gray-900')}>
              我的資料
            </NavLink>
            {isInRole('admin') && (
              <>
                <NavLink to="/users" className={({ isActive }) => (isActive ? 'font-semibold' : 'text-gray-600 hover:text-gray-900')}>
                  使用者
                </NavLink>
                <NavLink to="/departments" className={({ isActive }) => (isActive ? 'font-semibold' : 'text-gray-600 hover:text-gray-900')}>
                  部門
                </NavLink>
              </>
            )}
            <span className="text-gray-500">{user?.displayName}</span>
            <button onClick={handleLogout} className="px-3 py-1 rounded bg-gray-200 hover:bg-gray-300 text-sm">登出</button>
          </nav>
        </div>
      </header>
      <main className="flex-1">
        <div className="max-w-6xl mx-auto px-6 py-6">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

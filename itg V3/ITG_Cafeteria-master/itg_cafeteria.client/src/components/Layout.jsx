import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Layout({ children }) {
  const { user, logout, isAuthenticated, isSystemAdmin } = useAuth();

  return (
    <div className="app-layout">
      <header className="app-header">
        <Link to="/" className="brand">
          <span className="brand-icon">🍽️</span>
          <span>ITG Cafeteria</span>
        </Link>
        <nav className="app-nav">
          <Link to="/">Menu</Link>
          {isAuthenticated ? (
            <>
              <Link to="/portal">Portal</Link>
              {isSystemAdmin() && <Link to="/portal/users">Users</Link>}
              <span className="nav-user">{user?.displayName}</span>
              <button type="button" className="btn-ghost" onClick={logout}>Logout</button>
            </>
          ) : (
            <Link to="/login" className="btn-primary-sm">Staff Login</Link>
          )}
        </nav>
      </header>
      <main className="app-main">{children}</main>
      <footer className="app-footer">
        <p>ITG Cafeteria Menu System</p>
      </footer>
    </div>
  );
}

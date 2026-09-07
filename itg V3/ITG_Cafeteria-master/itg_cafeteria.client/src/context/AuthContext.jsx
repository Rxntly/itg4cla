import { createContext, useContext, useState } from 'react';
import { Navigate } from 'react-router-dom';

const AuthContext = createContext(null);

const ROLES = {
  SystemAdmin: 'SystemAdmin',
  CafeteriaEditor: 'CafeteriaEditor',
  CafeteriaPublisher: 'CafeteriaPublisher',
};

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const stored = localStorage.getItem('user');
    return stored ? JSON.parse(stored) : null;
  });

  const login = (data) => {
    localStorage.setItem('token', data.token);
    const userData = {
      username: data.username,
      email: data.email,
      displayName: data.displayName,
      roles: data.roles || [],
    };
    localStorage.setItem('user', JSON.stringify(userData));
    setUser(userData);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  };

  const isAuthenticated = !!user && !!localStorage.getItem('token');

  const hasRole = (role) => user?.roles?.includes(role) ?? false;

  const canPublish = () =>
    hasRole(ROLES.CafeteriaPublisher) || hasRole(ROLES.SystemAdmin);

  const isSystemAdmin = () => hasRole(ROLES.SystemAdmin);

  const canAccessPortal = () =>
    hasRole(ROLES.CafeteriaEditor) ||
    hasRole(ROLES.CafeteriaPublisher) ||
    hasRole(ROLES.SystemAdmin);

  return (
    <AuthContext.Provider
      value={{
        user,
        login,
        logout,
        isAuthenticated,
        hasRole,
        canPublish,
        isSystemAdmin,
        canAccessPortal,
        ROLES,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

export function ProtectedRoute({ children }) {
  const { isAuthenticated, canAccessPortal } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!canAccessPortal()) return <Navigate to="/login" replace />;
  return children;
}

export function AdminRoute({ children }) {
  const { isAuthenticated, isSystemAdmin } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!isSystemAdmin()) return <Navigate to="/portal" replace />;
  return children;
}

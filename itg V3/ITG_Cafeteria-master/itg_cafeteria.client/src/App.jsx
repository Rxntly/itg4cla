import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, ProtectedRoute, AdminRoute } from './context/AuthContext';
import PublicMenuPage from './pages/PublicMenuPage';
import LoginPage from './pages/LoginPage';
import PortalPage from './pages/PortalPage';
import MenuEditorPage from './pages/MenuEditorPage';
import UsersAdminPage from './pages/UsersAdminPage';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<PublicMenuPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/portal"
            element={
              <ProtectedRoute>
                <PortalPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/portal/edit/:weekStart"
            element={
              <ProtectedRoute>
                <MenuEditorPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/portal/users"
            element={
              <AdminRoute>
                <UsersAdminPage />
              </AdminRoute>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, formatDateISO, getWeekStart } from '../api/client';
import { useAuth } from '../context/AuthContext';
import Layout from '../components/Layout';
import WeekSelector from '../components/WeekSelector';

export default function PortalPage() {
  const { isSystemAdmin } = useAuth();
  const [menus, setMenus] = useState([]);
  const [weekStart, setWeekStart] = useState(formatDateISO(getWeekStart()));
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getMenus()
      .then(setMenus)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  const weekMenu = menus.find((m) => m.weekStartDate === weekStart);

  return (
    <Layout>
      <div className="portal-page">
        <div className="portal-header">
          <h1>Cafeteria Dashboard</h1>
          <div className="portal-header-actions">
            {isSystemAdmin() && (
              <Link to="/portal/users" className="btn-secondary">
                Manage Users
              </Link>
            )}
            <Link to={`/portal/edit/${weekStart}`} className="btn-primary">
              {weekMenu ? 'Edit Week Menu' : 'Create Week Menu'}
            </Link>
          </div>
        </div>

        <WeekSelector weekStart={weekStart} onChange={setWeekStart} />

        {weekMenu ? (
          <div className="week-status card">
            <div className="status-row">
              <span>Status:</span>
              <span className={`badge badge-${weekMenu.status.toLowerCase()}`}>{weekMenu.status}</span>
            </div>
            <p>Last updated: {new Date(weekMenu.updatedAt).toLocaleString()}</p>
            {weekMenu.publishedAt && (
              <p>Published: {new Date(weekMenu.publishedAt).toLocaleString()}</p>
            )}
            <Link to={`/portal/edit/${weekStart}`} className="btn-secondary">Open Editor</Link>
          </div>
        ) : (
          <div className="card empty-card">
            <p>No menu for this week yet.</p>
          </div>
        )}

        <section className="card">
          <h2>All Menus</h2>
          {loading ? (
            <p>Loading...</p>
          ) : menus.length === 0 ? (
            <p className="empty-message">No menus created yet.</p>
          ) : (
            <div className="menu-list">
              {menus.map((m) => (
                <Link key={m.id} to={`/portal/edit/${m.weekStartDate}`} className="menu-list-item">
                  <span>
                    {new Date(m.weekStartDate + 'T00:00:00').toLocaleDateString()} –
                    {new Date(m.weekEndDate + 'T00:00:00').toLocaleDateString()}
                  </span>
                  <span className={`badge badge-${m.status.toLowerCase()}`}>{m.status}</span>
                </Link>
              ))}
            </div>
          )}
        </section>
      </div>
    </Layout>
  );
}

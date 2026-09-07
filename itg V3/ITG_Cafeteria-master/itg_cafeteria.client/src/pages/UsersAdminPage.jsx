import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ROLE_LABELS } from '../api/client';
import Layout from '../components/Layout';

const CAFETERIA_ROLES = ['CafeteriaEditor', 'CafeteriaPublisher'];

const emptyForm = {
  username: '',
  email: '',
  displayName: '',
  password: '',
  roles: ['CafeteriaEditor'],
};

export default function UsersAdminPage() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editingUser, setEditingUser] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);

  const loadUsers = () => {
    setLoading(true);
    api.getUsers()
      .then(setUsers)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadUsers();
  }, []);

  const openCreate = () => {
    setEditingUser(null);
    setForm(emptyForm);
    setShowForm(true);
    setError('');
    setMessage('');
  };

  const openEdit = (user) => {
    setEditingUser(user);
    setForm({
      username: user.username,
      email: user.email,
      displayName: user.displayName,
      password: '',
      roles: user.roles.filter((r) => CAFETERIA_ROLES.includes(r)),
    });
    setShowForm(true);
    setError('');
    setMessage('');
  };

  const toggleRole = (role) => {
    setForm((prev) => ({
      ...prev,
      roles: prev.roles.includes(role)
        ? prev.roles.filter((r) => r !== role)
        : [...prev.roles, role],
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setError('');
    setMessage('');
    try {
      if (editingUser) {
        await api.updateUser(editingUser.id, {
          email: form.email,
          displayName: form.displayName,
          password: form.password || null,
          roles: form.roles,
        });
        setMessage('User updated successfully.');
      } else {
        await api.createUser(form);
        setMessage('User created successfully.');
      }
      setShowForm(false);
      loadUsers();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (user) => {
    if (!window.confirm(`Deactivate user "${user.username}"?`)) return;
    setError('');
    setMessage('');
    try {
      await api.deactivateUser(user.id);
      setMessage(`User "${user.username}" deactivated.`);
      loadUsers();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <Layout>
      <div className="portal-page">
        <div className="portal-header">
          <div>
            <Link to="/portal" className="btn-ghost">← Dashboard</Link>
            <h1>User Management</h1>
          </div>
          <button type="button" className="btn-primary" onClick={openCreate}>
            Create User
          </button>
        </div>

        {message && <div className="alert alert-success">{message}</div>}
        {error && <div className="alert alert-error">{error}</div>}

        <section className="card">
          <h2>Users</h2>
          {loading ? (
            <p>Loading...</p>
          ) : users.length === 0 ? (
            <p className="empty-message">No users found.</p>
          ) : (
            <div className="user-table-wrap">
              <table className="user-table">
                <thead>
                  <tr>
                    <th>Username</th>
                    <th>Display Name</th>
                    <th>Email</th>
                    <th>Roles</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id} className={!user.isActive ? 'user-inactive' : ''}>
                      <td>{user.username}</td>
                      <td>{user.displayName}</td>
                      <td>{user.email}</td>
                      <td>
                        {user.roles.map((role) => (
                          <span key={role} className="role-badge">
                            {ROLE_LABELS[role] || role}
                          </span>
                        ))}
                      </td>
                      <td>
                        <span className={`badge badge-${user.isActive ? 'published' : 'draft'}`}>
                          {user.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="user-actions">
                        {user.isActive && user.username !== 'SystemAdmin' && (
                          <>
                            <button type="button" className="btn-secondary btn-sm" onClick={() => openEdit(user)}>
                              Edit
                            </button>
                            <button type="button" className="btn-ghost btn-sm" onClick={() => handleDeactivate(user)}>
                              Deactivate
                            </button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        {showForm && (
          <div className="modal-overlay" onClick={() => setShowForm(false)}>
            <div className="modal card user-form-modal" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h2>{editingUser ? 'Edit User' : 'Create User'}</h2>
                <button type="button" className="btn-ghost modal-close" onClick={() => setShowForm(false)}>×</button>
              </div>
              <form onSubmit={handleSubmit} className="user-form">
                {!editingUser && (
                  <label>
                    Username
                    <input
                      type="text"
                      value={form.username}
                      onChange={(e) => setForm({ ...form, username: e.target.value })}
                      required
                    />
                  </label>
                )}
                <label>
                  Display Name
                  <input
                    type="text"
                    value={form.displayName}
                    onChange={(e) => setForm({ ...form, displayName: e.target.value })}
                    required
                  />
                </label>
                <label>
                  Email
                  <input
                    type="email"
                    value={form.email}
                    onChange={(e) => setForm({ ...form, email: e.target.value })}
                    required
                  />
                </label>
                <label>
                  {editingUser ? 'New Password (optional)' : 'Password'}
                  <input
                    type="password"
                    value={form.password}
                    onChange={(e) => setForm({ ...form, password: e.target.value })}
                    required={!editingUser}
                  />
                </label>
                <fieldset className="role-fieldset">
                  <legend>Roles</legend>
                  {CAFETERIA_ROLES.map((role) => (
                    <label key={role} className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={form.roles.includes(role)}
                        onChange={() => toggleRole(role)}
                      />
                      {ROLE_LABELS[role]}
                    </label>
                  ))}
                </fieldset>
                <div className="modal-actions">
                  <button type="button" className="btn-ghost" onClick={() => setShowForm(false)}>Cancel</button>
                  <button type="submit" className="btn-primary" disabled={saving || form.roles.length === 0}>
                    {saving ? 'Saving...' : editingUser ? 'Update User' : 'Create User'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
}

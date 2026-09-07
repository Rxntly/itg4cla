const API_BASE = '';

async function request(path, options = {}) {
  const token = localStorage.getItem('token');
  const headers = {
    ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });

  if (response.status === 401) {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    if (!path.includes('/auth/login')) {
      window.location.href = '/login';
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new Error('You do not have permission to perform this action.');
    }
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(error.message || 'Request failed');
  }

  if (response.status === 204) return null;
  return response.json();
}

export const api = {
  login: (username, password) =>
    request('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),

  getMenus: () => request('/api/menus'),
  getMenu: (id) => request(`/api/menus/${id}`),
  getMenuByWeek: (weekStart) => request(`/api/menus/week/${weekStart}`),
  createMenu: (data) => request('/api/menus', { method: 'POST', body: JSON.stringify(data) }),
  updateMenu: (id, data) => request(`/api/menus/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  publishMenu: (id) => request(`/api/menus/${id}/publish`, { method: 'POST' }),
  deleteMenu: (id) => request(`/api/menus/${id}`, { method: 'DELETE' }),

  previewImport: (file) => {
    const form = new FormData();
    form.append('file', file);
    return request('/api/import/preview', { method: 'POST', body: form });
  },
  saveImport: (data) => request('/api/import/save', { method: 'POST', body: JSON.stringify(data) }),

  getUsers: () => request('/api/users'),
  getUser: (id) => request(`/api/users/${id}`),
  createUser: (data) => request('/api/users', { method: 'POST', body: JSON.stringify(data) }),
  updateUser: (id, data) => request(`/api/users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deactivateUser: (id) => request(`/api/users/${id}/deactivate`, { method: 'POST' }),

  getTodayMenu: () => request('/api/public/today'),
  getMenuByDate: (date) => request(`/api/public/date/${date}`),
  getWeekMenu: (weekStart) => request(`/api/public/week/${weekStart}`),
  getPublishedWeeks: () => request('/api/public/weeks'),
};

export function formatDateISO(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function getWeekStart(date = new Date()) {
  const d = new Date(date);
  const day = d.getDay();
  const diff = day === 0 ? 6 : day - 1;
  d.setDate(d.getDate() - diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

export function addDays(date, days) {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

export const WEEKDAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

export const ROLE_LABELS = {
  SystemAdmin: 'System Administrator',
  CafeteriaEditor: 'Cafeteria Editor',
  CafeteriaPublisher: 'Cafeteria Publisher',
};

export function emptyWeekDays() {
  return WEEKDAYS.map((_, i) => ({
    dayOfWeek: i + 1,
    dayName: WEEKDAYS[i],
    nodes: [],
  }));
}

export function createNode(label = '') {
  return { label, sortOrder: 0, children: [] };
}

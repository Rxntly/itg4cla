import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, WEEKDAYS, emptyWeekDays, createNode, formatDateISO, getWeekStart } from '../api/client';
import { useAuth } from '../context/AuthContext';
import Layout from '../components/Layout';
import MenuTree from '../components/MenuTree';
import ExcelUpload from '../components/ExcelUpload';
import WeekSelector from '../components/WeekSelector';
import ImportPreviewModal from '../components/ImportPreviewModal';

function countDayItems(nodes) {
  if (!nodes?.length) return 0;
  return nodes.reduce((sum, meal) =>
    sum + (meal.children?.reduce((s, cat) => s + (cat.children?.length || 0), 0) || 0), 0);
}

export default function MenuEditorPage() {
  const { canPublish } = useAuth();
  const { weekStart: weekParam } = useParams();
  const [weekStart, setWeekStart] = useState(weekParam || formatDateISO(getWeekStart()));
  const [menuId, setMenuId] = useState(null);
  const [status, setStatus] = useState('Draft');
  const [selectedDay, setSelectedDay] = useState(1);
  const [days, setDays] = useState(emptyWeekDays());
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [importPreview, setImportPreview] = useState(null);
  const [importLoading, setImportLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [showImport, setShowImport] = useState(false);

  const mergeDays = useCallback((serverDays) => {
    const base = emptyWeekDays();
    return base.map((d) => {
      const found = serverDays.find(
        (sd) => sd.dayOfWeek === d.dayOfWeek || sd.dayName === d.dayName
      );
      return found ? { ...d, nodes: found.nodes || [], id: found.id } : d;
    });
  }, []);

  const loadMenu = useCallback(async (week) => {
    setLoading(true);
    setMessage('');
    try {
      const menu = await api.getMenuByWeek(week);
      if (menu) {
        setMenuId(menu.id);
        setStatus(menu.status);
        setDays(menu.days?.length ? mergeDays(menu.days) : emptyWeekDays());
      } else {
        setMenuId(null);
        setStatus('Draft');
        setDays(emptyWeekDays());
      }
    } catch {
      setMenuId(null);
      setDays(emptyWeekDays());
    } finally {
      setLoading(false);
    }
  }, [mergeDays]);

  useEffect(() => {
    loadMenu(weekStart);
  }, [weekStart, loadMenu]);

  const currentDay = days.find((d) => d.dayOfWeek === selectedDay);

  const updateTree = (path, node) => {
    setDays((prev) => {
      const next = prev.map((d) => ({ ...d, nodes: [...d.nodes] }));
      const day = next.find((d) => d.dayOfWeek === selectedDay);
      if (!day) return prev;

      if (path.length === 1) {
        if (node === null) day.nodes.splice(path[0], 1);
        else day.nodes[path[0]] = node;
        return next;
      }

      let target = day.nodes;
      for (let i = 0; i < path.length - 1; i++) {
        target = target[path[i]].children;
      }
      const idx = path[path.length - 1];
      if (node === null) target.splice(idx, 1);
      else target[idx] = node;
      return next;
    });
  };

  const addMealPeriod = () => {
    setDays((prev) =>
      prev.map((d) =>
        d.dayOfWeek === selectedDay
          ? { ...d, nodes: [...d.nodes, createNode('New Meal')] }
          : d
      )
    );
  };

  const addCategory = () => {
    setDays((prev) =>
      prev.map((d) => {
        if (d.dayOfWeek !== selectedDay) return d;
        const nodes = [...d.nodes];
        if (nodes.length === 0) {
          nodes.push(createNode('Breakfast'));
        }
        const meal = nodes[nodes.length - 1];
        meal.children = [...(meal.children || []), createNode('New Category')];
        return { ...d, nodes };
      })
    );
  };

  const buildSavePayload = (publish) => ({
    weekStartDate: weekStart,
    publish,
    days: days.map((d) => ({
      dayOfWeek: d.dayOfWeek,
      nodes: d.nodes,
    })),
  });

  const handleSave = async (publish = false) => {
    setSaving(true);
    setMessage('');
    try {
      const payload = buildSavePayload(publish);
      const result = menuId
        ? await api.updateMenu(menuId, payload)
        : await api.createMenu(payload);
      setMenuId(result.id);
      setStatus(result.status);
      setMessage(publish ? 'Menu published successfully!' : 'Draft saved successfully!');
      setDays(mergeDays(result.days));
    } catch (err) {
      setMessage(err.message || 'Save failed');
    } finally {
      setSaving(false);
    }
  };

  const handleExcelPreview = async (file) => {
    setImportLoading(true);
    setMessage('');
    try {
      const preview = await api.previewImport(file);
      setImportPreview(preview);
      setDays(mergeDays(preview.days));
      if (preview.detectedWeekStart) {
        setWeekStart(preview.detectedWeekStart);
      }
      setShowImport(true);
      setMessage('Menu imported — review the structure below, edit if needed, then save.');
    } catch (err) {
      setMessage(err.message || 'Import failed');
    } finally {
      setImportLoading(false);
    }
  };

  const closeImport = () => {
    setShowImport(false);
    setImportPreview(null);
  };

  const handleImportSave = async (publish) => {
    if (!importPreview) return;
    setSaving(true);
    try {
      const result = await api.saveImport({
        previewId: importPreview.previewId,
        weekStartDate: weekStart,
        days: days.map((d) => ({ dayOfWeek: d.dayOfWeek, nodes: d.nodes })),
        publish,
      });
      setMenuId(result.id);
      setStatus(result.status);
      setImportPreview(null);
      setShowImport(false);
      setDays(mergeDays(result.days));
      setMessage(publish ? 'Imported and published!' : 'Imported and saved as draft!');
    } catch (err) {
      setMessage(err.message || 'Save failed');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Layout>
      <div className="editor-page">
        <div className="editor-header">
          <Link to="/portal" className="btn-ghost">← Dashboard</Link>
          <h1>Menu Editor</h1>
          <span className={`badge badge-${status.toLowerCase()}`}>{status}</span>
        </div>

        <WeekSelector weekStart={weekStart} onChange={setWeekStart} />

        {message && (
          <div className={`alert ${message.toLowerCase().includes('fail') ? 'alert-error' : 'alert-success'}`}>
            {message}
          </div>
        )}

        <div className="editor-grid">
          <aside className="editor-sidebar card">
            <h3>Upload Weekly Menu</h3>
            <ExcelUpload onPreview={handleExcelPreview} loading={importLoading} />
            <div className="format-guide">
              <h4>Holcom menu format</h4>
              <ul>
                <li>Excel sheet named <strong>Menu</strong> or PDF export</li>
                <li>Columns B–F = Mon–Fri</li>
                <li><strong>Breakfast</strong> then categories</li>
                <li>Items start with <strong>.</strong> (dot)</li>
                <li>Lunch section after 2nd title block</li>
              </ul>
              <p className="hint-text">See <code>Menu/EXCEL_FORMAT.md</code> for full details.</p>
            </div>
          </aside>

          <section className="editor-main card">
            <div className="day-tabs">
              {WEEKDAYS.map((name, i) => {
                const dow = i + 1;
                const day = days.find((d) => d.dayOfWeek === dow);
                const count = countDayItems(day?.nodes);
                return (
                  <button
                    key={name}
                    type="button"
                    className={`day-tab ${selectedDay === dow ? 'active' : ''}`}
                    onClick={() => setSelectedDay(dow)}
                  >
                    {name.slice(0, 3)}
                    {count > 0 && <span className="tab-count">{count}</span>}
                  </button>
                );
              })}
            </div>

            {loading ? (
              <div className="loading-state">Loading...</div>
            ) : (
              <>
                <div className="editor-toolbar">
                  <button type="button" className="btn-secondary" onClick={addMealPeriod}>
                    + Meal Period
                  </button>
                  <button type="button" className="btn-secondary" onClick={addCategory}>
                    + Category
                  </button>
                </div>
                {currentDay?.nodes?.length > 0 ? (
                  <MenuTree nodes={currentDay.nodes} onChange={updateTree} editable defaultExpanded />
                ) : (
                  <div className="empty-editor">
                    <p className="empty-message">No menu for {WEEKDAYS[selectedDay - 1]} yet.</p>
                    <p className="hint-text">Upload a Holcom Excel/PDF menu or add categories manually.</p>
                  </div>
                )}
              </>
            )}
          </section>
        </div>

        <div className="editor-actions">
          <button type="button" className="btn-secondary" onClick={() => handleSave(false)} disabled={saving}>
            {saving ? 'Saving...' : 'Save Draft'}
          </button>
          {canPublish() && (
            <button type="button" className="btn-primary" onClick={() => handleSave(true)} disabled={saving}>
              {saving ? 'Publishing...' : 'Publish Menu'}
            </button>
          )}
        </div>

        {showImport && importPreview && (
          <ImportPreviewModal
            preview={importPreview}
            onClose={closeImport}
            onApplyAndEdit={closeImport}
            onSaveDraft={() => handleImportSave(false)}
            onPublish={() => handleImportSave(true)}
            saving={saving}
            canPublish={canPublish()}
          />
        )}
      </div>
    </Layout>
  );
}

import { useState } from 'react';
import { WEEKDAYS } from '../api/client';
import MenuTree from './MenuTree';

export default function ImportPreviewModal({
  preview,
  onClose,
  onApplyAndEdit,
  onSaveDraft,
  onPublish,
  saving,
  canPublish = false,
}) {
  const [previewDay, setPreviewDay] = useState(1);

  const dayData = preview.days?.find((d) => d.dayOfWeek === previewDay)
    || preview.days?.[previewDay - 1];

  const nodes = dayData?.nodes || [];

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal card import-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Import Preview</h2>
          <button type="button" className="btn-ghost modal-close" onClick={onClose}>×</button>
        </div>

        {preview.detectedWeekStart && (
          <p className="import-week-label">
            Week: <strong>{preview.detectedWeekStart}</strong>
            {preview.detectedWeekEnd && <> → <strong>{preview.detectedWeekEnd}</strong></>}
            {preview.sheetName && <span className="import-meta"> · Sheet: {preview.sheetName}</span>}
          </p>
        )}

        {preview.summary && (
          <div className="import-summary">
            <div className="import-stat">
              <span className="stat-value">{preview.summary.totalDays}</span>
              <span className="stat-label">Days</span>
            </div>
            <div className="import-stat">
              <span className="stat-value">{preview.summary.totalCategories}</span>
              <span className="stat-label">Categories</span>
            </div>
            <div className="import-stat">
              <span className="stat-value">{preview.summary.totalItems}</span>
              <span className="stat-label">Items</span>
            </div>
          </div>
        )}

        {preview.warnings?.length > 0 && (
          <div className="alert alert-warning">
            {preview.warnings.map((w, i) => <p key={i}>{w}</p>)}
          </div>
        )}

        <p className="import-hint">
          Menu structure has been loaded into the editor. Review each day below, make any changes, then save.
        </p>

        <div className="day-tabs">
          {WEEKDAYS.map((name, i) => {
            const dow = i + 1;
            const day = preview.days?.find((d) => d.dayOfWeek === dow);
            const itemCount = countItems(day?.nodes);
            return (
              <button
                key={name}
                type="button"
                className={`day-tab ${previewDay === dow ? 'active' : ''}`}
                onClick={() => setPreviewDay(dow)}
              >
                {name.slice(0, 3)}
                {itemCount > 0 && <span className="tab-count">{itemCount}</span>}
              </button>
            );
          })}
        </div>

        <div className="import-preview-content">
          <h4>{WEEKDAYS[previewDay - 1]}</h4>
          {nodes.length > 0 ? (
            <MenuTree nodes={nodes} defaultExpanded />
          ) : (
            <p className="empty-message">No items for this day.</p>
          )}
        </div>

        <div className="modal-actions">
          <button type="button" className="btn-ghost" onClick={onClose}>Cancel</button>
          <button type="button" className="btn-secondary" onClick={onApplyAndEdit}>
            Continue Editing
          </button>
          <button type="button" className="btn-secondary" onClick={onSaveDraft} disabled={saving}>
            {saving ? 'Saving...' : 'Save Draft'}
          </button>
          {canPublish && (
            <button type="button" className="btn-primary" onClick={onPublish} disabled={saving}>
              {saving ? 'Publishing...' : 'Publish Menu'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function countItems(nodes) {
  if (!nodes?.length) return 0;
  return nodes.reduce((sum, meal) =>
    sum + (meal.children?.reduce((s, cat) => s + (cat.children?.length || 0), 0) || 0), 0);
}

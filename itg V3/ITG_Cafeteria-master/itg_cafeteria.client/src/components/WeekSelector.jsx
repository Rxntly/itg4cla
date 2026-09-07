import { formatDateISO, addDays } from '../api/client';

export default function WeekSelector({ weekStart, onChange }) {
  const start = new Date(weekStart + 'T00:00:00');
  const end = addDays(start, 4);

  const shift = (weeks) => {
    const d = addDays(start, weeks * 7);
    onChange(formatDateISO(d));
  };

  const goToday = () => {
    const today = new Date();
    const day = today.getDay();
    const diff = day === 0 ? 6 : day - 1;
    today.setDate(today.getDate() - diff);
    onChange(formatDateISO(today));
  };

  const formatDisplay = (d) =>
    d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });

  return (
    <div className="week-selector">
      <button type="button" className="btn-secondary" onClick={() => shift(-1)}>← Prev</button>
      <div className="week-display">
        <span className="week-range">
          {formatDisplay(start)} – {formatDisplay(end)}
        </span>
        <button type="button" className="btn-link" onClick={goToday}>This week</button>
      </div>
      <button type="button" className="btn-secondary" onClick={() => shift(1)}>Next →</button>
    </div>
  );
}

import { useEffect, useState } from 'react';
import { api, formatDateISO, getWeekStart, WEEKDAYS } from '../api/client';
import Layout from '../components/Layout';
import MenuTree from '../components/MenuTree';
import WeekSelector from '../components/WeekSelector';

export default function PublicMenuPage() {
  const [weekStart, setWeekStart] = useState(formatDateISO(getWeekStart()));
  const [selectedDay, setSelectedDay] = useState(() => {
    const today = new Date().getDay();
    return today >= 1 && today <= 5 ? today : 1;
  });
  const [weekData, setWeekData] = useState(null);
  const [todayMenu, setTodayMenu] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getTodayMenu().then(setTodayMenu).catch(console.error);
  }, []);

  useEffect(() => {
    setLoading(true);
    api.getWeekMenu(weekStart)
      .then(setWeekData)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [weekStart]);

  const currentWeekStart = formatDateISO(getWeekStart());
  const isCurrentWeek = weekStart === currentWeekStart;
  const todayDow = new Date().getDay();
  const showTodayHighlight = isCurrentWeek && todayDow >= 1 && todayDow <= 5;

  const selectedDayData = weekData?.days?.find((d) => {
    const dow = WEEKDAYS.indexOf(d.dayName) + 1;
    return dow === selectedDay || d.dayOfWeek === selectedDay;
  });

  return (
    <Layout>
      <div className="public-menu">
        {showTodayHighlight && todayMenu && (
          <section className="today-highlight card">
            <div className="card-header">
              <h1>Today&apos;s Menu</h1>
              <span className="badge badge-today">{todayMenu.dayName}</span>
            </div>
            {todayMenu.nodes?.length > 0 ? (
              <MenuTree nodes={todayMenu.nodes} defaultExpanded />
            ) : (
              <p className="empty-message">No menu published for today yet.</p>
            )}
          </section>
        )}

        <section className="week-section card">
          <div className="card-header">
            <h2>Weekly Menu</h2>
          </div>
          <WeekSelector weekStart={weekStart} onChange={setWeekStart} />

          <div className="day-tabs">
            {WEEKDAYS.map((name, i) => {
              const dow = i + 1;
              const isToday = showTodayHighlight && dow === todayDow;
              return (
                <button
                  key={name}
                  type="button"
                  className={`day-tab ${selectedDay === dow ? 'active' : ''} ${isToday ? 'is-today' : ''}`}
                  onClick={() => setSelectedDay(dow)}
                >
                  {name.slice(0, 3)}
                  {isToday && <span className="today-dot" />}
                </button>
              );
            })}
          </div>

          {loading ? (
            <div className="loading-state">Loading menu...</div>
          ) : (
            <div className="day-menu-content">
              <h3>{WEEKDAYS[selectedDay - 1]}</h3>
              {selectedDayData?.nodes?.length > 0 ? (
                <MenuTree nodes={selectedDayData.nodes} defaultExpanded />
              ) : (
                <p className="empty-message">No menu available for this day.</p>
              )}
            </div>
          )}
        </section>
      </div>
    </Layout>
  );
}

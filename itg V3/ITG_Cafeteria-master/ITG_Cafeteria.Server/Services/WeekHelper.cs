namespace ITG_Cafeteria.Server.Services;

public static class WeekHelper
{
    public static DateOnly GetWeekStart(DateOnly date)
    {
        var dow = date.DayOfWeek;
        var diff = dow == DayOfWeek.Sunday ? 6 : (int)dow - (int)DayOfWeek.Monday;
        return date.AddDays(-diff);
    }
}

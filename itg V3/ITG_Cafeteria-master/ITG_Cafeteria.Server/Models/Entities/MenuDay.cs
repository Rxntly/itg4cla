namespace ITG_Cafeteria.Server.Models.Entities;

public class MenuDay
{
    public int Id { get; set; }
    public int WeeklyMenuId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }

    public WeeklyMenu WeeklyMenu { get; set; } = null!;
    public ICollection<MenuNode> Nodes { get; set; } = new List<MenuNode>();
}

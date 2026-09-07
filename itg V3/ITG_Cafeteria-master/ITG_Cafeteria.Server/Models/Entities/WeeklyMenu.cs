using ITG_Cafeteria.Server.Models.Enums;

namespace ITG_Cafeteria.Server.Models.Entities;

public class WeeklyMenu
{
    public int Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public MenuStatus Status { get; set; } = MenuStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public int CreatedByUserId { get; set; }

    public CafeteriaUser CreatedByUser { get; set; } = null!;
    public ICollection<MenuDay> Days { get; set; } = new List<MenuDay>();
}

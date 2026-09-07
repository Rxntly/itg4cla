using ITG_Cafeteria.Server.Models.Enums;

namespace ITG_Cafeteria.Server.Models.DTOs;

public class WeeklyMenuDto
{
    public int Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate => WeekStartDate.AddDays(4);
    public MenuStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<MenuDayDto> Days { get; set; } = new();
}

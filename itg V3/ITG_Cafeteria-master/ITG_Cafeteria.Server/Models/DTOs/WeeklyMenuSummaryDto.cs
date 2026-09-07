namespace ITG_Cafeteria.Server.Models.DTOs;

public class WeeklyMenuSummaryDto
{
    public int Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

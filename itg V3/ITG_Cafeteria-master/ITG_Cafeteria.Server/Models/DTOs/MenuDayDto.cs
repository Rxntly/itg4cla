namespace ITG_Cafeteria.Server.Models.DTOs;

public class MenuDayDto
{
    public int? Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName => DayOfWeek.ToString();
    public List<MenuNodeDto> Nodes { get; set; } = new();
}

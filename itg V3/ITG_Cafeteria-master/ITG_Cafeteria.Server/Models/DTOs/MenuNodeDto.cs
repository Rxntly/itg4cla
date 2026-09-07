namespace ITG_Cafeteria.Server.Models.DTOs;

public class MenuNodeDto
{
    public int? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<MenuNodeDto> Children { get; set; } = new();
}

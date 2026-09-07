namespace ITG_Cafeteria.Server.Models.Entities;

/// <summary>
/// Adjacency-list node supporting unlimited menu hierarchy depth.
/// Root nodes have ParentId = null (e.g. Breakfast, Lunch).
/// </summary>
public class MenuNode
{
    public int Id { get; set; }
    public int MenuDayId { get; set; }
    public int? ParentId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public MenuDay MenuDay { get; set; } = null!;
    public MenuNode? Parent { get; set; }
    public ICollection<MenuNode> Children { get; set; } = new List<MenuNode>();
}

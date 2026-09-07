using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Models.Entities;

namespace ITG_Cafeteria.Server.Services;

public static class MenuMapper
{
    public static WeeklyMenuDto ToDto(WeeklyMenu menu)
    {
        return new WeeklyMenuDto
        {
            Id = menu.Id,
            WeekStartDate = menu.WeekStartDate,
            Status = menu.Status,
            CreatedAt = menu.CreatedAt,
            UpdatedAt = menu.UpdatedAt,
            PublishedAt = menu.PublishedAt,
            Days = menu.Days
                .OrderBy(d => d.DayOfWeek)
                .Select(ToDayDto)
                .ToList()
        };
    }

    public static MenuDayDto ToDayDto(MenuDay day)
    {
        var allNodes = day.Nodes.ToList();
        var childrenLookup = allNodes
            .Where(n => n.ParentId.HasValue)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.SortOrder).ToList());

        List<MenuNodeDto> BuildChildren(int parentId) =>
            childrenLookup.TryGetValue(parentId, out var children)
                ? children.Select(ToNodeDtoWithLookup).ToList()
                : new List<MenuNodeDto>();

        MenuNodeDto ToNodeDtoWithLookup(MenuNode node) => new()
        {
            Id = node.Id,
            Label = node.Label,
            SortOrder = node.SortOrder,
            Children = BuildChildren(node.Id)
        };

        var roots = allNodes
            .Where(n => n.ParentId == null)
            .OrderBy(n => n.SortOrder)
            .Select(ToNodeDtoWithLookup)
            .ToList();

        return new MenuDayDto
        {
            Id = day.Id,
            DayOfWeek = day.DayOfWeek,
            Nodes = roots
        };
    }

    public static MenuNodeDto ToNodeDto(MenuNode node)
    {
        return new MenuNodeDto
        {
            Id = node.Id,
            Label = node.Label,
            SortOrder = node.SortOrder,
            Children = node.Children
                .OrderBy(c => c.SortOrder)
                .Select(ToNodeDto)
                .ToList()
        };
    }
}

using ITG_Cafeteria.Server.Data;
using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Models.Entities;
using ITG_Cafeteria.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ITG_Cafeteria.Server.Services;

public interface IMenuService
{
    Task<List<WeeklyMenuSummaryDto>> GetAllMenusAsync();
    Task<WeeklyMenuDto?> GetMenuByIdAsync(int id);
    Task<WeeklyMenuDto?> GetMenuByWeekAsync(DateOnly weekStart, bool publishedOnly = false);
    Task<WeeklyMenuDto> SaveMenuAsync(int userId, SaveWeeklyMenuRequest request, int? existingMenuId = null);
    Task<WeeklyMenuDto?> PublishMenuAsync(int id);
    Task<bool> DeleteMenuAsync(int id);
    Task<PublicMenuResponse?> GetPublicMenuForDateAsync(DateOnly date);
    Task<PublicWeekResponse?> GetPublicWeekAsync(DateOnly weekStart);
}

public class MenuService : IMenuService
{
    private readonly CafeteriaDbContext _db;

    public MenuService(CafeteriaDbContext db)
    {
        _db = db;
    }

    public async Task<List<WeeklyMenuSummaryDto>> GetAllMenusAsync()
    {
        return await _db.WeeklyMenus
            .OrderByDescending(m => m.WeekStartDate)
            .Select(m => new WeeklyMenuSummaryDto
            {
                Id = m.Id,
                WeekStartDate = m.WeekStartDate,
                WeekEndDate = m.WeekStartDate.AddDays(4),
                Status = m.Status.ToString(),
                UpdatedAt = m.UpdatedAt,
                PublishedAt = m.PublishedAt
            })
            .ToListAsync();
    }

    public async Task<WeeklyMenuDto?> GetMenuByIdAsync(int id)
    {
        var menu = await LoadMenuGraph()
            .FirstOrDefaultAsync(m => m.Id == id);

        return menu == null ? null : MenuMapper.ToDto(menu);
    }

    public async Task<WeeklyMenuDto?> GetMenuByWeekAsync(DateOnly weekStart, bool publishedOnly = false)
    {
        var normalized = WeekHelper.GetWeekStart(weekStart);
        var query = LoadMenuGraph().Where(m => m.WeekStartDate == normalized);

        if (publishedOnly)
        {
            query = query.Where(m => m.Status == MenuStatus.Published);
        }

        var menu = await query.FirstOrDefaultAsync();
        return menu == null ? null : MenuMapper.ToDto(menu);
    }

    public async Task<WeeklyMenuDto> SaveMenuAsync(int userId, SaveWeeklyMenuRequest request, int? existingMenuId = null)
    {
        var weekStart = WeekHelper.GetWeekStart(request.WeekStartDate);
        WeeklyMenu menu;

        if (existingMenuId.HasValue)
        {
            menu = await _db.WeeklyMenus
                .Include(m => m.Days)
                .ThenInclude(d => d.Nodes)
                .FirstAsync(m => m.Id == existingMenuId.Value);
        }
        else
        {
            menu = await _db.WeeklyMenus
                .Include(m => m.Days)
                .ThenInclude(d => d.Nodes)
                .FirstOrDefaultAsync(m => m.WeekStartDate == weekStart) ?? new WeeklyMenu
            {
                WeekStartDate = weekStart,
                CreatedByUserId = userId,
                Days = CreateEmptyWeekDays()
            };

            if (menu.Id == 0)
            {
                _db.WeeklyMenus.Add(menu);
            }
        }

        menu.WeekStartDate = weekStart;
        menu.UpdatedAt = DateTime.UtcNow;

        if (request.Publish)
        {
            menu.Status = MenuStatus.Published;
            menu.PublishedAt = DateTime.UtcNow;
        }
        else if (menu.Status != MenuStatus.Published)
        {
            menu.Status = MenuStatus.Draft;
        }

        await SyncDaysAsync(menu, request.Days);
        await _db.SaveChangesAsync();

        var saved = await LoadMenuGraph().FirstAsync(m => m.Id == menu.Id);
        return MenuMapper.ToDto(saved);
    }

    public async Task<WeeklyMenuDto?> PublishMenuAsync(int id)
    {
        var menu = await _db.WeeklyMenus.FindAsync(id);
        if (menu == null) return null;

        menu.Status = MenuStatus.Published;
        menu.PublishedAt = DateTime.UtcNow;
        menu.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetMenuByIdAsync(id);
    }

    public async Task<bool> DeleteMenuAsync(int id)
    {
        var menu = await _db.WeeklyMenus.FindAsync(id);
        if (menu == null) return false;

        _db.WeeklyMenus.Remove(menu);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PublicMenuResponse?> GetPublicMenuForDateAsync(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return null;
        }

        var weekStart = WeekHelper.GetWeekStart(date);
        var menu = await LoadMenuGraph()
            .Where(m => m.WeekStartDate == weekStart && m.Status == MenuStatus.Published)
            .FirstOrDefaultAsync();

        if (menu == null) return null;

        var day = menu.Days.FirstOrDefault(d => d.DayOfWeek == date.DayOfWeek);
        if (day == null) return null;

        var today = DateOnly.FromDateTime(DateTime.Today);
        return new PublicMenuResponse
        {
            Date = date,
            DayOfWeek = date.DayOfWeek,
            IsToday = date == today,
            Nodes = day.Nodes
                .Where(n => n.ParentId == null)
                .OrderBy(n => n.SortOrder)
                .Select(MenuMapper.ToNodeDto)
                .ToList()
        };
    }

    public async Task<PublicWeekResponse?> GetPublicWeekAsync(DateOnly weekStart)
    {
        var normalized = WeekHelper.GetWeekStart(weekStart);
        var menu = await LoadMenuGraph()
            .Where(m => m.WeekStartDate == normalized && m.Status == MenuStatus.Published)
            .FirstOrDefaultAsync();

        if (menu == null) return null;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var days = new List<PublicMenuResponse>();

        for (var i = 0; i < 5; i++)
        {
            var date = normalized.AddDays(i);
            var dayOfWeek = date.DayOfWeek;
            var day = menu.Days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);

            days.Add(new PublicMenuResponse
            {
                Date = date,
                DayOfWeek = dayOfWeek,
                IsToday = date == today,
                Nodes = day?.Nodes
                    .Where(n => n.ParentId == null)
                    .OrderBy(n => n.SortOrder)
                    .Select(MenuMapper.ToNodeDto)
                    .ToList() ?? new List<MenuNodeDto>()
            });
        }

        return new PublicWeekResponse
        {
            WeekStartDate = normalized,
            WeekEndDate = normalized.AddDays(4),
            Days = days
        };
    }

    private IQueryable<WeeklyMenu> LoadMenuGraph() =>
        _db.WeeklyMenus
            .Include(m => m.Days)
            .ThenInclude(d => d.Nodes);

    private static List<MenuDay> CreateEmptyWeekDays() =>
        Enumerable.Range(0, 5)
            .Select(i => new MenuDay { DayOfWeek = DayOfWeek.Monday + i })
            .ToList();

    private async Task SyncDaysAsync(WeeklyMenu menu, List<MenuDayDto> dayDtos)
    {
        foreach (var dayDto in dayDtos)
        {
            var day = menu.Days.FirstOrDefault(d => d.DayOfWeek == dayDto.DayOfWeek);
            if (day == null)
            {
                day = new MenuDay { DayOfWeek = dayDto.DayOfWeek };
                menu.Days.Add(day);
            }

            _db.MenuNodes.RemoveRange(day.Nodes);
            day.Nodes.Clear();

            var flatNodes = BuildNodesFromTree(dayDto.Nodes, day);
            foreach (var node in flatNodes)
            {
                day.Nodes.Add(node);
            }
        }

        await Task.CompletedTask;
    }

    private static List<MenuNode> BuildNodesFromTree(List<MenuNodeDto> nodes, MenuDay day, MenuNode? parent = null)
    {
        var result = new List<MenuNode>();
        var order = 0;

        foreach (var dto in nodes)
        {
            if (string.IsNullOrWhiteSpace(dto.Label)) continue;

            var node = new MenuNode
            {
                MenuDay = day,
                Parent = parent,
                Label = dto.Label.Trim(),
                SortOrder = dto.SortOrder > 0 ? dto.SortOrder : order
            };

            result.Add(node);

            if (dto.Children.Count > 0)
            {
                result.AddRange(BuildNodesFromTree(dto.Children, day, node));
            }

            order++;
        }

        return result;
    }
}

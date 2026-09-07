using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITG_Cafeteria.Server.Controllers;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicMenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public PublicMenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet("today")]
    public async Task<ActionResult<PublicMenuResponse>> GetToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var menu = await _menuService.GetPublicMenuForDateAsync(today);

        if (menu == null)
        {
            return Ok(new PublicMenuResponse
            {
                Date = today,
                DayOfWeek = today.DayOfWeek,
                IsToday = true,
                Nodes = new List<MenuNodeDto>()
            });
        }

        return Ok(menu);
    }

    [HttpGet("date/{date}")]
    public async Task<ActionResult<PublicMenuResponse>> GetByDate(DateOnly date)
    {
        var menu = await _menuService.GetPublicMenuForDateAsync(date);
        if (menu == null)
        {
            return Ok(new PublicMenuResponse
            {
                Date = date,
                DayOfWeek = date.DayOfWeek,
                IsToday = date == DateOnly.FromDateTime(DateTime.Today),
                Nodes = new List<MenuNodeDto>()
            });
        }

        return Ok(menu);
    }

    [HttpGet("week/{weekStart}")]
    public async Task<ActionResult<PublicWeekResponse>> GetWeek(DateOnly weekStart)
    {
        var week = await _menuService.GetPublicWeekAsync(weekStart);
        if (week == null)
        {
            var normalized = WeekHelper.GetWeekStart(weekStart);
            return Ok(new PublicWeekResponse
            {
                WeekStartDate = normalized,
                WeekEndDate = normalized.AddDays(4),
                Days = Enumerable.Range(0, 5).Select(i =>
                {
                    var date = normalized.AddDays(i);
                    return new PublicMenuResponse
                    {
                        Date = date,
                        DayOfWeek = date.DayOfWeek,
                        IsToday = date == DateOnly.FromDateTime(DateTime.Today),
                        Nodes = new List<MenuNodeDto>()
                    };
                }).ToList()
            });
        }

        return Ok(week);
    }

    [HttpGet("weeks")]
    public async Task<ActionResult<List<WeeklyMenuSummaryDto>>> GetPublishedWeeks()
    {
        var all = await _menuService.GetAllMenusAsync();
        return Ok(all.Where(m => m.Status == "Published").ToList());
    }
}

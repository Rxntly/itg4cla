using System.Security.Claims;
using ITG_Cafeteria.Server.Authorization;
using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITG_Cafeteria.Server.Controllers;

[ApiController]
[Route("api/menus")]
[Authorize(Roles = RoleNames.MenuStaff)]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<ActionResult<List<WeeklyMenuSummaryDto>>> GetAll()
    {
        return Ok(await _menuService.GetAllMenusAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WeeklyMenuDto>> GetById(int id)
    {
        var menu = await _menuService.GetMenuByIdAsync(id);
        return menu == null ? NotFound() : Ok(menu);
    }

    [HttpGet("week/{weekStart}")]
    public async Task<ActionResult<WeeklyMenuDto>> GetByWeek(DateOnly weekStart)
    {
        var menu = await _menuService.GetMenuByWeekAsync(weekStart);
        return menu == null ? NotFound() : Ok(menu);
    }

    [HttpPost]
    public async Task<ActionResult<WeeklyMenuDto>> Create([FromBody] SaveWeeklyMenuRequest request)
    {
        if (request.Publish && !CanPublish())
            return Forbid();

        var userId = GetUserId();
        var menu = await _menuService.SaveMenuAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = menu.Id }, menu);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WeeklyMenuDto>> Update(int id, [FromBody] SaveWeeklyMenuRequest request)
    {
        if (request.Publish && !CanPublish())
            return Forbid();

        var menu = await _menuService.SaveMenuAsync(GetUserId(), request, id);
        return Ok(menu);
    }

    [HttpPost("{id:int}/publish")]
    [Authorize(Roles = RoleNames.PublisherOnly)]
    public async Task<ActionResult<WeeklyMenuDto>> Publish(int id)
    {
        var menu = await _menuService.PublishMenuAsync(id);
        return menu == null ? NotFound() : Ok(menu);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _menuService.DeleteMenuAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool CanPublish() =>
        User.IsInRole(RoleNames.CafeteriaPublisher) || User.IsInRole(RoleNames.SystemAdmin);
}

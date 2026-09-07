using System.Security.Claims;
using ITG_Cafeteria.Server.Authorization;
using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITG_Cafeteria.Server.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Roles = RoleNames.MenuStaff)]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly IMenuService _menuService;

    public ImportController(IExcelImportService excelImportService, IMenuService menuService)
    {
        _excelImportService = excelImportService;
        _menuService = menuService;
    }

    [HttpPost("preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImportPreviewResponse>> Preview(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Please upload a menu file." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xlsm" and not ".xls" and not ".pdf")
        {
            return BadRequest(new { message = "Only Excel (.xlsx, .xlsm, .xls) and PDF (.pdf) files are supported." });
        }

        await using var stream = file.OpenReadStream();
        var preview = await _excelImportService.ParseAsync(stream, file.FileName);
        return Ok(preview);
    }

    [HttpPost("save")]
    public async Task<ActionResult<WeeklyMenuDto>> Save([FromBody] ImportSaveRequest request)
    {
        if (request.Publish && !CanPublish())
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.PreviewId))
        {
            return BadRequest(new { message = "Preview ID is required." });
        }

        var preview = _excelImportService.GetPreview(request.PreviewId);
        if (preview == null && request.Days.Count == 0)
        {
            return BadRequest(new { message = "Import preview expired. Please upload the file again." });
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var saveRequest = new SaveWeeklyMenuRequest
        {
            WeekStartDate = request.WeekStartDate,
            Days = request.Days,
            Publish = request.Publish
        };

        var existing = await _menuService.GetMenuByWeekAsync(request.WeekStartDate);
        var menu = existing != null
            ? await _menuService.SaveMenuAsync(userId, saveRequest, existing.Id)
            : await _menuService.SaveMenuAsync(userId, saveRequest);

        _excelImportService.RemovePreview(request.PreviewId);
        return Ok(menu);
    }

    private bool CanPublish() =>
        User.IsInRole(RoleNames.CafeteriaPublisher) || User.IsInRole(RoleNames.SystemAdmin);
}

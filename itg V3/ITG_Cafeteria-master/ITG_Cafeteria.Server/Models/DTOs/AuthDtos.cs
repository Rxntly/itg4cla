namespace ITG_Cafeteria.Server.Models.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class SaveWeeklyMenuRequest
{
    public DateOnly WeekStartDate { get; set; }
    public List<MenuDayDto> Days { get; set; } = new();
    public bool Publish { get; set; }
}

public class ImportPreviewResponse
{
    public string PreviewId { get; set; } = string.Empty;
    public DateOnly? DetectedWeekStart { get; set; }
    public DateOnly? DetectedWeekEnd { get; set; }
    public string? SheetName { get; set; }
    public string Format { get; set; } = "HolcomGrid";
    public List<MenuDayDto> Days { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ImportSummaryDto? Summary { get; set; }
}

public class ImportSaveRequest
{
    public string PreviewId { get; set; } = string.Empty;
    public DateOnly WeekStartDate { get; set; }
    public List<MenuDayDto> Days { get; set; } = new();
    public bool Publish { get; set; }
}

public class PublicMenuResponse
{
    public DateOnly Date { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName => DayOfWeek.ToString();
    public bool IsToday { get; set; }
    public List<MenuNodeDto> Nodes { get; set; } = new();
}

public class PublicWeekResponse
{
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public List<PublicMenuResponse> Days { get; set; } = new();
}

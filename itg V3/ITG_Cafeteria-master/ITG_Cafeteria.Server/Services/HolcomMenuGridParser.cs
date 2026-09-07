using System.Data;
using System.Text.RegularExpressions;
using ITG_Cafeteria.Server.Models.DTOs;

namespace ITG_Cafeteria.Server.Services;

/// <summary>
/// Parses Holcom cafeteria weekly menu grid data (see Menu/EXCEL_FORMAT.md).
/// Grid layout: columns B–F = Monday–Friday; categories are plain text; items start with ".".
/// </summary>
public static class HolcomMenuGridParser
{
    // DataTable columns are 0-based: A=0, B=1 (Monday) … F=5 (Friday)
    private const int FirstDayColumn = 1;
    private const int LastDayColumn = 5;

    private static readonly string[] DayNames =
        ["monday", "tuesday", "wednesday", "thursday", "friday"];

    private static readonly string[] MonthNames =
        ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];

    public static HolcomParseResult Parse(DataTable table, string fileName, string? sourceName = null)
    {
        var warnings = new List<string>();
        var weekStart = DetectWeekStart(table, fileName, warnings);
        var titleRows = FindTitleRows(table);

        var days = new Dictionary<DayOfWeek, MenuDayDto>();
        for (var col = FirstDayColumn; col <= LastDayColumn; col++)
        {
            var dayOfWeek = DayOfWeek.Monday + (col - FirstDayColumn);
            days[dayOfWeek] = new MenuDayDto
            {
                DayOfWeek = dayOfWeek,
                Nodes = ParseDayColumn(table, col, titleRows, warnings)
            };
        }

        return new HolcomParseResult
        {
            WeekStartDate = weekStart,
            WeekEndDate = weekStart?.AddDays(4),
            SheetName = sourceName ?? table.TableName,
            Days = days.Values.OrderBy(d => d.DayOfWeek).ToList(),
            Warnings = warnings
        };
    }

    public static DataTable? SelectWorksheet(DataSet dataSet, List<string> warnings)
    {
        if (dataSet.Tables.Count == 0)
        {
            warnings.Add("The workbook contains no worksheets.");
            return null;
        }

        var menuSheet = dataSet.Tables.Cast<DataTable>()
            .FirstOrDefault(t => t.TableName.Equals("Menu", StringComparison.OrdinalIgnoreCase));

        if (menuSheet != null)
        {
            return menuSheet;
        }

        warnings.Add("Worksheet 'Menu' not found — using the first sheet.");
        return dataSet.Tables[0];
    }

    public static List<MenuDayDto> CreateEmptyDays() =>
        Enumerable.Range(0, 5)
            .Select(i => new MenuDayDto
            {
                DayOfWeek = DayOfWeek.Monday + i,
                Nodes = new List<MenuNodeDto>()
            })
            .ToList();

    private static List<MenuNodeDto> ParseDayColumn(
        DataTable table,
        int columnIndex,
        List<int> titleRows,
        List<string> warnings)
    {
        var roots = new List<MenuNodeDto>();
        MenuNodeDto? breakfastNode = null;
        MenuNodeDto? lunchNode = null;
        MenuNodeDto? currentCategory = null;
        var lunchSection = false;
        var seenBreakfast = false;

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            if (IsDayHeaderRow(table, rowIndex))
            {
                continue;
            }

            if (titleRows.Contains(rowIndex))
            {
                if (seenBreakfast)
                {
                    lunchSection = true;
                    lunchNode = GetOrCreateMeal(roots, "Lunch");
                    currentCategory = null;
                }
                continue;
            }

            var raw = GetCell(table, rowIndex, columnIndex);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (IsFooterText(raw))
            {
                continue;
            }

            if (IsDayName(raw))
            {
                continue;
            }

            if (IsTitleText(raw))
            {
                continue;
            }

            var label = raw.Trim();

            if (label.Equals("Breakfast", StringComparison.OrdinalIgnoreCase))
            {
                seenBreakfast = true;
                breakfastNode = GetOrCreateMeal(roots, "Breakfast");
                currentCategory = null;
                lunchSection = false;
                continue;
            }

            var activeMeal = lunchSection ? lunchNode : breakfastNode;
            if (activeMeal == null)
            {
                continue;
            }

            if (IsItemLabel(label))
            {
                var itemLabel = StripItemPrefix(label);
                if (string.IsNullOrWhiteSpace(itemLabel))
                {
                    continue;
                }

                if (currentCategory == null)
                {
                    currentCategory = CreateCategory("General", activeMeal.Children.Count);
                    activeMeal.Children.Add(currentCategory);
                    warnings.Add($"Row {rowIndex + 1}: item '{itemLabel}' added under 'General' (no category above it).");
                }

                currentCategory.Children.Add(CreateItem(itemLabel, currentCategory.Children.Count));
                continue;
            }

            currentCategory = CreateCategory(label, activeMeal.Children.Count);
            activeMeal.Children.Add(currentCategory);
        }

        return roots;
    }

    private static List<int> FindTitleRows(DataTable table)
    {
        var rows = new List<int>();
        for (var r = 0; r < table.Rows.Count; r++)
        {
            var parts = new List<string>();
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var val = GetCell(table, r, c);
                if (!string.IsNullOrWhiteSpace(val))
                {
                    parts.Add(val);
                }
            }

            if (parts.Count == 0)
            {
                continue;
            }

            var combined = string.Join(" ", parts);
            if (IsTitleText(combined)
                || parts.Any(IsTitleText)
                || (TryParseWeekStart(combined, out _) && combined.Length < 100))
            {
                rows.Add(r);
            }
        }

        return rows;
    }

    private static bool IsDayHeaderRow(DataTable table, int rowIndex)
    {
        var matches = 0;
        for (var col = FirstDayColumn; col <= LastDayColumn; col++)
        {
            var val = GetCell(table, rowIndex, col);
            if (IsDayName(val))
            {
                matches++;
            }
        }

        return matches >= 3;
    }

    private static string GetCell(DataTable table, int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= table.Rows.Count)
        {
            return string.Empty;
        }

        if (columnIndex < 0 || columnIndex >= table.Columns.Count)
        {
            return string.Empty;
        }

        return table.Rows[rowIndex][columnIndex]?.ToString()?.Trim() ?? string.Empty;
    }

    private static MenuNodeDto GetOrCreateMeal(List<MenuNodeDto> roots, string label)
    {
        var existing = roots.FirstOrDefault(n =>
            n.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            return existing;
        }

        var meal = new MenuNodeDto
        {
            Label = label,
            SortOrder = roots.Count,
            Children = new List<MenuNodeDto>()
        };
        roots.Add(meal);
        return meal;
    }

    private static MenuNodeDto CreateCategory(string label, int sortOrder) => new()
    {
        Label = label.Trim(),
        SortOrder = sortOrder,
        Children = new List<MenuNodeDto>()
    };

    private static MenuNodeDto CreateItem(string label, int sortOrder) => new()
    {
        Label = label.Trim(),
        SortOrder = sortOrder,
        Children = new List<MenuNodeDto>()
    };

    public static bool IsItemLabel(string label) =>
        label.TrimStart().StartsWith('.');

    public static string StripItemPrefix(string label) =>
        Regex.Replace(label.Trim(), @"^\.\s*", string.Empty).Trim();

    public static bool IsDayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = Regex.Replace(value.Trim(), @"[^a-zA-Z]", "").ToLowerInvariant();
        return DayNames.Contains(normalized);
    }

    public static bool IsTitleText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("Holcom Cafeteria Menu", StringComparison.OrdinalIgnoreCase)
               || value.Contains("Holcom Cafeteria", StringComparison.OrdinalIgnoreCase)
                  && Regex.IsMatch(value, @"\d{1,2}", RegexOptions.IgnoreCase);
    }

    public static bool IsFooterText(string value)
    {
        if (value.Length > 180)
        {
            return true;
        }

        var lower = value.ToLowerInvariant();
        return lower.Contains("lbp ttc")
               || lower.Contains("reserve your plate")
               || lower.Contains("healthy menu: grilled chicken breast platter")
               || lower.Contains("to ensure the best service");
    }

    public static bool IsStructuralLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return true;
        }

        if (IsDayName(label) || IsTitleText(label) || IsFooterText(label))
        {
            return true;
        }

        return label.Equals("Breakfast", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMealOrCategoryLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label) || IsItemLabel(label))
        {
            return false;
        }

        if (IsStructuralLabel(label))
        {
            return false;
        }

        // Short category headers rarely end with a comma; wrapped dish descriptions do.
        if (label.Contains(',') || label.Length > 80)
        {
            return false;
        }

        // PDF line wraps often continue with lowercase fragments (e.g. "hommos", "sauce").
        if (char.IsLower(label.TrimStart()[0]))
        {
            return false;
        }

        return true;
    }

    private static DateOnly? DetectWeekStart(DataTable table, string fileName, List<string> warnings)
    {
        for (var r = 0; r < Math.Min(8, table.Rows.Count); r++)
        {
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var cell = GetCell(table, r, c);
                if (TryParseWeekStart(cell, out var date))
                {
                    return date;
                }
            }
        }

        if (TryParseWeekStart(fileName, out var fromFile))
        {
            warnings.Add("Week dates detected from filename.");
            return fromFile;
        }

        warnings.Add("Could not detect week dates from the file — using the current week.");
        return WeekHelper.GetWeekStart(DateOnly.FromDateTime(DateTime.Today));
    }

    public static bool TryParseWeekStart(string text, out DateOnly weekStart)
    {
        weekStart = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var rangeMatch = Regex.Match(text,
            @"(?i)(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s*(\d{1,2})\s*(?:st|nd|rd|th)?\s*[-–]\s*(?:(?:jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s*)?(\d{1,2})");

        if (rangeMatch.Success)
        {
            var month = ParseMonth(rangeMatch.Groups[1].Value);
            var day = int.Parse(rangeMatch.Groups[2].Value);
            var year = ExtractYear(text) ?? DateTime.Today.Year;
            if (month.HasValue)
            {
                weekStart = new DateOnly(year, month.Value, day);
                weekStart = WeekHelper.GetWeekStart(weekStart);
                return true;
            }
        }

        var fileMatch = Regex.Match(text,
            @"(?i)(\d{1,2})\s*(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)?\s+to\s+(\d{1,2})\s*(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)",
            RegexOptions.IgnoreCase);

        if (fileMatch.Success)
        {
            var startDay = int.Parse(fileMatch.Groups[1].Value);
            var startMonthToken = fileMatch.Groups[2].Success ? fileMatch.Groups[2].Value : fileMatch.Groups[4].Value;
            var month = ParseMonth(startMonthToken);
            var year = ExtractYear(text) ?? DateTime.Today.Year;
            if (month.HasValue)
            {
                weekStart = new DateOnly(year, month.Value, startDay);
                weekStart = WeekHelper.GetWeekStart(weekStart);
                return true;
            }
        }

        return false;
    }

    private static int? ParseMonth(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var normalized = Regex.Replace(token.ToLowerInvariant(), @"[^a-z]", "");
        for (var i = 0; i < MonthNames.Length; i++)
        {
            if (normalized.StartsWith(MonthNames[i], StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return null;
    }

    private static int? ExtractYear(string text)
    {
        var yearMatch = Regex.Match(text, @"\b(20\d{2})\b");
        return yearMatch.Success ? int.Parse(yearMatch.Groups[1].Value) : null;
    }
}

public class HolcomParseResult
{
    public DateOnly? WeekStartDate { get; set; }
    public DateOnly? WeekEndDate { get; set; }
    public string? SheetName { get; set; }
    public List<MenuDayDto> Days { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public ImportSummaryDto Summary => new()
    {
        TotalDays = Days.Count(d => d.Nodes.Count > 0),
        TotalCategories = Days.Sum(CountCategories),
        TotalItems = Days.Sum(CountItems)
    };

    private static int CountCategories(MenuDayDto day) =>
        day.Nodes.Sum(meal => meal.Children.Count);

    private static int CountItems(MenuDayDto day) =>
        day.Nodes.Sum(meal => meal.Children.Sum(cat => cat.Children.Count));
}

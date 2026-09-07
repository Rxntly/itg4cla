using ITG_Cafeteria.Server.Services;

var menuDir = args.Length > 0 ? args[0] : Path.Combine("..", "..", "..", "..", "Menu");
var files = Directory.GetFiles(menuDir)
    .Where(f => f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f);

foreach (var file in files)
{
    Console.WriteLine(new string('=', 80));
    Console.WriteLine(Path.GetFileName(file));
    await using var stream = File.OpenRead(file);
    var result = Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        ? HolcomMenuPdfParser.Parse(stream, Path.GetFileName(file))
        : HolcomMenuExcelParser.Parse(stream, Path.GetFileName(file));

    Console.WriteLine($"Source: {result.SheetName}");
    Console.WriteLine($"Week: {result.WeekStartDate:yyyy-MM-dd} to {result.WeekEndDate:yyyy-MM-dd}");
    Console.WriteLine($"Summary: {result.Summary.TotalDays} days, {result.Summary.TotalCategories} categories, {result.Summary.TotalItems} items");
    foreach (var w in result.Warnings) Console.WriteLine($"  WARN: {w}");

    foreach (var day in result.Days.Where(d => d.Nodes.Count > 0))
    {
        Console.WriteLine($"\n{day.DayOfWeek}:");
        PrintTree(day.Nodes, 1);
    }
}

static void PrintTree(List<ITG_Cafeteria.Server.Models.DTOs.MenuNodeDto> nodes, int indent)
{
    foreach (var n in nodes)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}{n.Label} ({n.Children.Count} children)");
        PrintTree(n.Children, indent + 1);
    }
}

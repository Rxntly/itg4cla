using System.Data;
using System.Text;
using ExcelDataReader;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var menuDir = args.Length > 0 ? args[0] : Path.Combine("..", "..", "Menu");
var files = Directory.GetFiles(menuDir, "*.xlsx");

foreach (var file in files)
{
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"FILE: {Path.GetFileName(file)}");
    Console.WriteLine(new string('=', 80));

    await using var stream = File.OpenRead(file);
    using var reader = ExcelReaderFactory.CreateReader(stream);
    var ds = reader.AsDataSet(new ExcelDataSetConfiguration
    {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
    });

    foreach (DataTable table in ds.Tables)
    {
        Console.WriteLine($"\n--- Sheet: {table.TableName} (rows={table.Rows.Count}, cols={table.Columns.Count}) ---");

        var maxRows = Math.Min(table.Rows.Count, 150);
        for (var r = 0; r < maxRows; r++)
        {
            var parts = new List<string>();
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var val = table.Rows[r][c]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(val)) continue;
                parts.Add($"C{c + 1}:{val}");
            }
            if (parts.Count > 0)
                Console.WriteLine($"R{r + 1}: {string.Join(" | ", parts)}");
        }
    }
}

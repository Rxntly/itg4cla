using ClosedXML.Excel;

var workbook = new XLWorkbook();
var ws = workbook.Worksheets.Add("Weekly Menu");

var rows = new[]
{
    new[] { "Monday", "", "" },
    new[] { "", "Breakfast", "" },
    new[] { "", "", "Baguette" },
    new[] { "", "", "Cheese Baguette" },
    new[] { "", "", "Turkey Baguette" },
    new[] { "", "", "Tuna Baguette" },
    new[] { "", "Croissant", "" },
    new[] { "", "", "Cheese Croissant" },
    new[] { "", "", "Chocolate Croissant" },
    new[] { "", "Lunch", "" },
    new[] { "", "", "Main Course" },
    new[] { "", "", "Grilled Chicken" },
    new[] { "", "", "Beef Steak" },
    new[] { "", "", "Salads" },
    new[] { "", "", "Caesar" },
    new[] { "", "", "Greek" },
    new[] { "Tuesday", "", "" },
    new[] { "", "Breakfast", "" },
    new[] { "", "", "Baguette" },
    new[] { "", "", "Option 1" },
    new[] { "", "", "Option 2" },
    new[] { "", "", "Option 3" },
    new[] { "", "Lunch", "" },
    new[] { "", "", "Main Course" },
    new[] { "", "", "Chicken" },
    new[] { "", "", "Beef" },
};

for (var i = 0; i < rows.Length; i++)
{
    for (var j = 0; j < rows[i].Length; j++)
    {
        ws.Cell(i + 1, j + 1).Value = rows[i][j];
    }
}

ws.Columns().AdjustToContents();
var outDir = Path.Combine("..", "samples");
Directory.CreateDirectory(outDir);
var path = Path.Combine(outDir, "weekly-menu-template.xlsx");
workbook.SaveAs(path);
Console.WriteLine($"Created {Path.GetFullPath(path)}");

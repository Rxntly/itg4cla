using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ITG_Cafeteria.Server.Services;

/// <summary>
/// Extracts Holcom cafeteria weekly menu grid data from PDF files and delegates
/// parsing to <see cref="HolcomMenuGridParser"/>.
/// </summary>
public static class HolcomMenuPdfParser
{
    private const int DayColumnCount = 5;
    private const double LineYTolerance = 6.0;

    public static HolcomParseResult Parse(Stream stream, string fileName)
    {
        var warnings = new List<string>();
        var table = BuildGridTable(stream, warnings);
        if (table == null)
        {
            return new HolcomParseResult
            {
                Warnings = warnings,
                Days = HolcomMenuGridParser.CreateEmptyDays()
            };
        }

        var result = HolcomMenuGridParser.Parse(table, fileName, "PDF");
        result.Warnings.InsertRange(0, warnings);
        return result;
    }

    private static DataTable? BuildGridTable(Stream stream, List<string> warnings)
    {
        using var document = PdfDocument.Open(stream);
        var words = document.GetPages()
            .SelectMany(page => page.GetWords().Select(w =>
                new PdfWord(page.Number, w.Text, w.BoundingBox.Left, w.BoundingBox.Bottom, w.BoundingBox.Width)))
            .ToList();

        if (words.Count == 0)
        {
            warnings.Add("The PDF contains no extractable text.");
            return null;
        }

        var lines = GroupWordsIntoLines(words);
        var boundaries = DetectColumnBoundaries(lines, warnings);
        if (boundaries == null)
        {
            warnings.Add("Could not detect weekday columns in the PDF.");
            return null;
        }

        var table = CreateTable();
        var openItemColumns = new bool[DayColumnCount];

        foreach (var line in lines)
        {
            var columnTexts = ExtractColumnTexts(line.Words, boundaries);
            NormalizeImplicitItems(columnTexts);

            if (IsFooterLine(columnTexts))
            {
                break;
            }

            if (IsSkippableLine(columnTexts))
            {
                openItemColumns = new bool[DayColumnCount];
                continue;
            }

            if (IsContinuationLine(columnTexts, openItemColumns))
            {
                MergeContinuation(table, columnTexts, openItemColumns);
                continue;
            }

            openItemColumns = new bool[DayColumnCount];
            var row = table.NewRow();
            row[0] = string.Empty;

            for (var col = 0; col < DayColumnCount; col++)
            {
                var text = columnTexts[col];
                row[col + 1] = text;

                if (!string.IsNullOrWhiteSpace(text) && HolcomMenuGridParser.IsItemLabel(text))
                {
                    openItemColumns[col] = true;
                }
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static DataTable CreateTable()
    {
        var table = new DataTable("PDF");
        for (var i = 0; i <= DayColumnCount; i++)
        {
            table.Columns.Add($"C{i}", typeof(string));
        }

        return table;
    }

    private static List<PdfLine> GroupWordsIntoLines(IEnumerable<PdfWord> words)
    {
        return words
            .GroupBy(w => (w.PageNumber, Y: Math.Round(w.Bottom / LineYTolerance) * LineYTolerance))
            .Select(g => new PdfLine(g.Key.PageNumber, g.Key.Y, g.OrderBy(w => w.Left).ToList()))
            .OrderBy(l => l.PageNumber)
            .ThenByDescending(l => l.Y)
            .ToList();
    }

    private static double[]? DetectColumnBoundaries(List<PdfLine> lines, List<string> warnings)
    {
        var dayPositions = new List<double[]>();

        foreach (var line in lines)
        {
            var positions = new double?[DayColumnCount];
            foreach (var word in line.Words)
            {
                var dayIndex = GetDayIndex(word.Text);
                if (dayIndex >= 0)
                {
                    positions[dayIndex] = word.Left;
                }
            }

            if (positions.Count(p => p.HasValue) >= 3)
            {
                dayPositions.Add(positions.Select(p => p ?? double.NaN).ToArray());
            }
        }

        if (dayPositions.Count == 0)
        {
            return null;
        }

        var averaged = new double[DayColumnCount];
        for (var i = 0; i < DayColumnCount; i++)
        {
            averaged[i] = dayPositions.Where(p => !double.IsNaN(p[i])).Average(p => p[i]);
        }

        var boundaries = new double[DayColumnCount + 1];
        boundaries[0] = 0;
        for (var i = 1; i < DayColumnCount; i++)
        {
            boundaries[i] = (averaged[i - 1] + averaged[i]) / 2.0;
        }

        boundaries[DayColumnCount] = double.MaxValue;
        return boundaries;
    }

    private static int GetDayIndex(string text)
    {
        if (!HolcomMenuGridParser.IsDayName(text))
        {
            return -1;
        }

        var normalized = Regex.Replace(text.Trim(), @"[^a-zA-Z]", "").ToLowerInvariant();
        return normalized switch
        {
            "monday" => 0,
            "tuesday" => 1,
            "wednesday" => 2,
            "thursday" => 3,
            "friday" => 4,
            _ => -1
        };
    }

    private static string[] ExtractColumnTexts(List<PdfWord> words, double[] boundaries)
    {
        var buckets = new List<PdfWord>[DayColumnCount];
        for (var i = 0; i < DayColumnCount; i++)
        {
            buckets[i] = new List<PdfWord>();
        }

        foreach (var word in words)
        {
            var center = word.Left + (word.Width / 2.0);
            var column = GetColumnIndex(center, boundaries);
            if (column >= 0)
            {
                buckets[column].Add(word);
            }
        }

        var texts = new string[DayColumnCount];
        for (var i = 0; i < DayColumnCount; i++)
        {
            texts[i] = JoinWords(buckets[i]);
        }

        return texts;
    }

    private static int GetColumnIndex(double x, double[] boundaries)
    {
        for (var i = 0; i < DayColumnCount; i++)
        {
            if (x >= boundaries[i] && x < boundaries[i + 1])
            {
                return i;
            }
        }

        return -1;
    }

    private static string JoinWords(List<PdfWord> words)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var word in words.OrderBy(w => w.Left))
        {
            if (builder.Length == 0)
            {
                builder.Append(word.Text);
                continue;
            }

            if (word.Text == "." || word.Text.StartsWith('.'))
            {
                builder.Append(word.Text);
            }
            else if (builder.Length > 0 && builder[^1] == '.')
            {
                builder.Append(word.Text);
            }
            else
            {
                builder.Append(' ').Append(word.Text);
            }
        }

        return builder.ToString().Trim();
    }

    private static void NormalizeImplicitItems(string[] columnTexts)
    {
        var dottedColumns = columnTexts.Count(t => !string.IsNullOrWhiteSpace(t) && HolcomMenuGridParser.IsItemLabel(t));
        if (dottedColumns == 0)
        {
            return;
        }

        for (var i = 0; i < columnTexts.Length; i++)
        {
            var text = columnTexts[i];
            if (string.IsNullOrWhiteSpace(text) || HolcomMenuGridParser.IsItemLabel(text))
            {
                continue;
            }

            if (HolcomMenuGridParser.IsMealOrCategoryLabel(text))
            {
                continue;
            }

            columnTexts[i] = ". " + text.Trim();
        }
    }

    private static bool IsSkippableLine(string[] columnTexts)
    {
        var combined = string.Join(' ', columnTexts.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (string.IsNullOrWhiteSpace(combined))
        {
            return true;
        }

        if (HolcomMenuGridParser.IsTitleText(combined))
        {
            return true;
        }

        return columnTexts.Count(t => !string.IsNullOrWhiteSpace(t) && HolcomMenuGridParser.IsDayName(t)) >= 3;
    }

    private static bool IsFooterLine(string[] columnTexts)
    {
        return columnTexts.Any(t => !string.IsNullOrWhiteSpace(t) && HolcomMenuGridParser.IsFooterText(t));
    }

    private static bool IsContinuationLine(string[] columnTexts, bool[] openItemColumns)
    {
        if (!openItemColumns.Any(open => open))
        {
            return false;
        }

        for (var i = 0; i < DayColumnCount; i++)
        {
            var text = columnTexts[i];
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (HolcomMenuGridParser.IsItemLabel(text) || HolcomMenuGridParser.IsMealOrCategoryLabel(text))
            {
                return false;
            }

            if (!openItemColumns[i])
            {
                return false;
            }
        }

        return columnTexts.Any(t => !string.IsNullOrWhiteSpace(t));
    }

    private static void MergeContinuation(DataTable table, string[] columnTexts, bool[] openItemColumns)
    {
        if (table.Rows.Count == 0)
        {
            return;
        }

        var row = table.Rows[^1];
        for (var i = 0; i < DayColumnCount; i++)
        {
            var text = columnTexts[i];
            if (string.IsNullOrWhiteSpace(text) || !openItemColumns[i])
            {
                continue;
            }

            var existing = row[i + 1]?.ToString() ?? string.Empty;
            row[i + 1] = string.IsNullOrWhiteSpace(existing) ? text : $"{existing} {text}";
        }
    }

    private sealed record PdfWord(int PageNumber, string Text, double Left, double Bottom, double Width);

    private sealed record PdfLine(int PageNumber, double Y, List<PdfWord> Words);
}

using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace ITG_Cafeteria.Server.Services;

/// <summary>
/// Reads Holcom cafeteria weekly menu Excel files and delegates grid parsing
/// to <see cref="HolcomMenuGridParser"/> (see Menu/EXCEL_FORMAT.md).
/// </summary>
public static class HolcomMenuExcelParser
{
    static HolcomMenuExcelParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static HolcomParseResult Parse(Stream stream, string fileName)
    {
        var warnings = new List<string>();

        // 💡 FIX: Copy the network stream into a dedicated, seekable MemoryStream
        // to prevent the unmanaged ExcelDataReader library from crashing the native debugger thread.
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0; // Reset position pointer to the beginning of the file data

        // Configure standard isolated tracking parameters
        var config = new ExcelReaderConfiguration()
        {
            FallbackEncoding = Encoding.UTF8,
            LeaveOpen = false
        };

        // Pass the safe memoryStream into the reader engine instead of the raw file stream wrapper
        using var reader = ExcelReaderFactory.CreateReader(memoryStream, config);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        var table = HolcomMenuGridParser.SelectWorksheet(dataSet, warnings);
        if (table == null)
        {
            return new HolcomParseResult
            {
                Warnings = warnings,
                Days = HolcomMenuGridParser.CreateEmptyDays()
            };
        }

        var result = HolcomMenuGridParser.Parse(table, fileName, table.TableName);
        result.Warnings.InsertRange(0, warnings);
        return result;
    }
}

using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<BookDto>>(json, options) ?? [];
            return new ImportResult<BookDto>(items, new List<string>());
        }
        catch (Exception ex)
        {
            return new ImportResult<BookDto>(new List<BookDto>(), new List<string> { $"помилка розбору JSON: {ex.Message}" });
        }
    }
}
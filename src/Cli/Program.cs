using Core.Dto;
using Core.Import;

if (args.Contains("--mixed"))
{
    string mixedPath = Path.Combine("data", "mixed.csv");

    if (!File.Exists(mixedPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(mixedPath)}");
        return 1;
    }

    MixedImportResult mixed = MixedCsvImporter.Load(mixedPath);

    Console.WriteLine($"Книги: {mixed.Books.Count}, Читачі: {mixed.Readers.Count}, Помилки: {mixed.Errors.Count}");

    foreach (BookDto b in mixed.Books)
        Console.WriteLine($"  [B] {b.Id,-6} {b.Title,-30} {b.Year}");

    foreach (ReaderDto r in mixed.Readers)
        Console.WriteLine($"  [R] {r.Id,-6} {r.Name}");

    if (mixed.Errors.Count > 0)
    {
        Console.WriteLine("Помилки:");
        foreach (string e in mixed.Errors)
            Console.WriteLine($"  ! {e}");
    }

    return 0;
}

string path = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();

// Додаткове завдання 1: обрати імпортер за розширенням файлу
ImportResult<BookDto> result = extension switch
{
    ".csv" => BookCsvImporter.Load(path),
    ".json" => BookJsonImporter.Load(path),
    _ => new ImportResult<BookDto>(new List<BookDto>(), new List<string> { $"непідтримуване розширення файлу: {extension}" })
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (BookDto b in result.Items.Take(5))
    Console.WriteLine($"  {b.Id,-6} {b.Isbn,-18} {b.Title,-30} {b.Year,5}  {b.Author}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

// Додаткове завдання 3: статистика одним рядком
int total = result.Items.Count + result.Errors.Count;
double errorPercent = total == 0 ? 0 : (double)result.Errors.Count / total * 100;
Console.WriteLine($"Статистика: усього {total}, прийнято {result.Items.Count}, пропущено {result.Errors.Count} ({errorPercent:F1}%)");

return 0;
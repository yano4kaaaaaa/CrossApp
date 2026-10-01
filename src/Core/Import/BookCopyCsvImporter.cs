using Core.Dto;

namespace Core.Import;

public static class BookCopyCsvImporter
{
    private const char Separator = ';';

    // Увага: тут перевіряється лише СТРУКТУРА рядка (кількість колонок, формат bool).
    // Змістовні правила (напр. непорожній ISBN) свідомо НЕ перевіряються тут —
    // це завдання доменного шару (BookCopy.Create), щоб продемонструвати два різних
    // рівні помилок: помилки розбору файлу і помилки порушення інваріантів.
    public static ImportResult<BookCopyDto> Load(string path)
    {
        var items = new List<BookCopyDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            switch (parts)
            {
                case { Length: < 3 }:
                    errors.Add($"рядок {number}: очікую 3 колонки, отримав {parts.Length}");
                    break;

                case { Length: > 3 }:
                    errors.Add($"рядок {number}: занадто багато колонок: {parts.Length}");
                    break;

                case [var id, var isbn, var issued] when !bool.TryParse(issued, out _):
                    errors.Add($"рядок {number}: значення '{issued}' не є true/false");
                    break;

                case [var id, var isbn, var issued]:
                    items.Add(new BookCopyDto(id, isbn, bool.Parse(issued)));
                    break;
            }
        }

        return new ImportResult<BookCopyDto>(items, errors);
    }
}
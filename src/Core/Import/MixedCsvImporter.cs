using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    private const char Separator = ';';

    // Рядки з префіксом "B" — книги, з префіксом "R" — читачі. Один switch, два різні типи результату.
    public static MixedImportResult Load(string path)
    {
        var books = new List<BookDto>();
        var readers = new List<ReaderDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            switch (parts)
            {
                case ["B", var id, var isbn, var title, var year, ..] when int.TryParse(year, out int y):
                    books.Add(new BookDto(id, isbn, title, y));
                    break;

                case ["R", var id, var name, ..]:
                    readers.Add(new ReaderDto(id, name));
                    break;

                case ["B", ..]:
                    errors.Add($"рядок {number}: некоректний формат рядка книги");
                    break;

                case ["R", ..]:
                    errors.Add($"рядок {number}: некоректний формат рядка читача");
                    break;

                default:
                    errors.Add($"рядок {number}: невідомий префікс типу");
                    break;
            }
        }

        return new MixedImportResult(books, readers, errors);
    }
}
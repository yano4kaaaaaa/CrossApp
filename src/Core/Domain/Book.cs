namespace Core.Domain;

// Видання каталогу (isbn, title, author, year, copies). Незмінна сутність: усі перевірки в Create.
public sealed class Book
{
    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public string Author { get; }
    public int Year { get; }
    public int Copies { get; }

    private Book(string id, string isbn, string title, string author, int year, int copies)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        Author = author;
        Year = year;
        Copies = copies;
    }

    public static Book Create(string id, string isbn, string title, string author, int year, int copies)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видання обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(isbn) || isbn.Trim().Length > 20)
            throw new ArgumentException("ISBN обов'язковий, до 20 символів", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            throw new ArgumentException("Назва обов'язкова, до 200 символів", nameof(title));

        if (string.IsNullOrWhiteSpace(author) || author.Trim().Length > 200)
            throw new ArgumentException("Автор обов'язковий, до 200 символів", nameof(author));

        int maxYear = DateTime.Today.Year + 1;
        if (year < 1450 || year > maxYear)
            throw new ArgumentOutOfRangeException(nameof(year), year, $"Рік має бути від 1450 до {maxYear}");

        if (copies < 0)
            throw new ArgumentOutOfRangeException(nameof(copies), copies, "Кількість примірників не може бути від'ємною");

        return new Book(id.Trim(), isbn.Trim(), title.Trim(), author.Trim(), year, copies);
    }
}
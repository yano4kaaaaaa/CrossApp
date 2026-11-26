using Core;
using Core.Abstractions;
using Core.Domain;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class EfBookCatalog(AppDbContext db) : IBookCatalog
{
    private const int MaxRegisteredCopies = 1000;

    private static string Key(string value) => value.Trim().ToLowerInvariant();

    private static Book ToDomain(BookRow row) =>
        Book.Create(row.Id, row.Isbn, row.Title, row.Author, row.Year, row.Copies);

    private static void Apply(BookRow row, Book book)
    {
        row.Isbn = book.Isbn;
        row.Title = book.Title;
        row.Author = book.Author;
        row.Year = book.Year;
        row.Copies = book.Copies;
        row.AuthorKey = Key(book.Author);
        row.SearchKey = Key($"{book.Title}\n{book.Author}\n{book.Isbn}");
    }

    public async Task<BookPage> SearchAsync(
        string? author, string? search, int skip, int take, CancellationToken ct = default)
    {
        IQueryable<BookRow> query = db.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(author))
        {
            string a = Key(author);
            query = query.Where(b => b.AuthorKey.Contains(a));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = Key(search);
            query = query.Where(b => b.SearchKey.Contains(s));
        }

        int total = await query.CountAsync(ct);
        List<BookRow> rows = await query
            .OrderBy(b => b.Title).ThenBy(b => b.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return new BookPage(rows.Select(ToDomain).ToList(), total);
    }

    public async Task<Book?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        BookRow? row = await db.Books.AsNoTracking().SingleOrDefaultAsync(b => b.Id == id, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task AddAsync(Book book, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(book);

        if (await db.Books.AnyAsync(b => b.Isbn == book.Isbn, ct))
            throw new DomainException($"Видання з ISBN {book.Isbn} уже існує.");

        if (await db.Books.AnyAsync(b => b.Id == book.Id, ct))
            throw new DomainException($"Видання з id={book.Id} уже існує.");

        var row = new BookRow { Id = book.Id };
        Apply(row, book);

        for (int i = 1; i <= Math.Min(book.Copies, MaxRegisteredCopies); i++)
            row.CopyRows.Add(new CopyRow { InventoryNumber = $"{book.Isbn}-{i:D3}", State = "Good", IsIssued = false });

        db.Books.Add(row);
        await SaveAsync(book.Isbn, ct);
    }

    public async Task UpdateAsync(Book book, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(book);

        BookRow? row = await db.Books.SingleOrDefaultAsync(b => b.Id == book.Id, ct);
        if (row is null)
            throw new DomainException($"Видання з id={book.Id} не знайдено.");

        if (await db.Books.AnyAsync(b => b.Id != book.Id && b.Isbn == book.Isbn, ct))
            throw new DomainException($"Видання з ISBN {book.Isbn} уже існує.");

        Apply(row, book);
        await SaveAsync(book.Isbn, ct);
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        BookRow? row = await db.Books.SingleOrDefaultAsync(b => b.Id == id, ct);
        if (row is null)
            return false;

        db.Books.Remove(row);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task SaveAsync(string isbn, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            throw new DomainException($"Видання з ISBN {isbn} уже існує.", ex);
        }
    }
}
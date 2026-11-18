using System.Collections.Concurrent;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Потокобезпечне сховище в пам'яті (реєструється як Singleton).
// Читання: ConcurrentDictionary; записи, що перевіряють унікальність ISBN, під lock.
public sealed class InMemoryBookCatalog : IBookCatalog
{
    private readonly ConcurrentDictionary<string, Book> _books = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public InMemoryBookCatalog(IEnumerable<Book>? seed = null)
    {
        foreach (Book b in seed ?? [])
            _books[b.Id] = b;
    }

    public Task<BookPage> SearchAsync(
        string? author, string? search, int skip, int take, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IEnumerable<Book> query = _books.Values;

        if (!string.IsNullOrWhiteSpace(author))
        {
            string a = author.Trim();
            query = query.Where(b => b.Author.Contains(a, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            query = query.Where(b =>
                b.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                b.Author.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                b.Isbn.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        // Детермінований порядок: назва, потім id.
        List<Book> ordered = query
            .OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(b => b.Id, StringComparer.Ordinal)
            .ToList();

        List<Book> items = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult(new BookPage(items, ordered.Count));
    }

    public Task<Book?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_books.GetValueOrDefault(id));
    }

    public Task AddAsync(Book book, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (IsbnTaken(book.Isbn, exceptId: null))
                throw new DomainException($"Видання з ISBN {book.Isbn} уже існує.");

            if (!_books.TryAdd(book.Id, book))
                throw new DomainException($"Видання з id={book.Id} уже існує.");
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Book book, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_books.ContainsKey(book.Id))
                throw new DomainException($"Видання з id={book.Id} не знайдено.");

            if (IsbnTaken(book.Isbn, exceptId: book.Id))
                throw new DomainException($"Видання з ISBN {book.Isbn} уже існує.");

            _books[book.Id] = book;
        }
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_books.TryRemove(id, out _));
    }

    private bool IsbnTaken(string isbn, string? exceptId) =>
        _books.Values.Any(b =>
            string.Equals(b.Isbn, isbn, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(b.Id, exceptId, StringComparison.OrdinalIgnoreCase));
}
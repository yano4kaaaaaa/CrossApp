using Core.Domain;

namespace Core.Abstractions;

// Сторінка результатів пошуку: елементи + загальна кількість (для пагінації).
public sealed record BookPage(IReadOnlyList<Book> Items, int Total);

// Асинхронний контракт сховища видань. API залежить лише від нього:
// на тижні 11 реалізацію замінять на EF Core + SQLite, маршрути й відповіді не зміняться.
public interface IBookCatalog
{
    Task<BookPage> SearchAsync(
        string? author, string? search, int skip, int take, CancellationToken ct = default);

    Task<Book?> GetByIdAsync(string id, CancellationToken ct = default);

    // Кидає DomainException, якщо ISBN уже існує.
    Task AddAsync(Book book, CancellationToken ct = default);

    // Кидає DomainException, якщо видання немає або ISBN зайнятий іншим виданням.
    Task UpdateAsync(Book book, CancellationToken ct = default);

    Task<bool> RemoveAsync(string id, CancellationToken ct = default);
}
using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class LendingService(IBookStore store)
{
    private readonly IBookStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public BookCopy AddBook(string isbn)
    {
        BookCopy copy = BookCopy.Create(Guid.NewGuid().ToString("N")[..8], isbn);
        _store.Add(copy); // унікальність id перевіряє сховище, коректність полів — Product.Create
        return copy;
    }

    public void IssueCopy(string id)
    {
        BookCopy copy = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        copy.Issue(); // інваріант сутності з тижня 4
        _store.Update(copy);
    }

    public void ReturnCopy(string id)
    {
        BookCopy copy = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        copy.Return();
        _store.Update(copy);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);

    // Додаткове завдання 2: пошук з довільним предикатом.
    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) =>
        _store.List().Where(predicate).ToList();
}
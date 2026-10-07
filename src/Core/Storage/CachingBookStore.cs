using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Додаткове завдання 1: декоратор. Приймає ІНШИЙ ICatalogStore/IBookStore у конструкторі —
// той самий контракт, нова поведінка (кешування List() між змінами), без жодних змін
// у LendingService чи в самих реалізаціях сховищ.
public sealed class CachingBookStore(IBookStore inner) : IBookStore
{
    private readonly IBookStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<BookCopy>? _cachedList;

    public IReadOnlyList<BookCopy> List()
    {
        _cachedList ??= _inner.List();
        return _cachedList;
    }

    public BookCopy? GetById(string id) => _inner.GetById(id);

    public void Add(BookCopy item)
    {
        _inner.Add(item);
        _cachedList = null; // інвалідація кешу після зміни
    }

    public void Update(BookCopy item)
    {
        _inner.Update(item);
        _cachedList = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) _cachedList = null;
        return removed;
    }
}
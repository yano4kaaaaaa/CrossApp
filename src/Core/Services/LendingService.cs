using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService(IBookStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly IBookStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly List<INotificationSink> _sinks = sinks?.ToList() ?? [];

    // Подія, а не public-поле делегата: ззовні можна лише += і -=, викликати може тільки сервіс.
    public event EventHandler<LendingChangedEventArgs>? Changed;

    // Єдине місце підняття події. Викликається ЛИШЕ після успішної зміни стану.
    private void OnChanged(
        BookCopy copy,
        LendingChangeKind kind,
        string? readerId = null,
        DateOnly? dueDate = null)
    {
        var args = new LendingChangedEventArgs(copy.Id, kind, readerId, dueDate);

        Changed?.Invoke(this, args);

        foreach (INotificationSink sink in _sinks)
            sink.Notify(args);
    }

    public BookCopy AddBook(string isbn)
    {
        BookCopy copy = BookCopy.Create(Guid.NewGuid().ToString("N")[..8], isbn);
        _store.Add(copy);
        OnChanged(copy, LendingChangeKind.BookAdded); // після зміни, не до
        return copy;
    }

    public void IssueCopy(string id, string readerId, int loanDays = 14)
    {
        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));

        if (loanDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(loanDays), loanDays,
                "Термін видачі має бути більшим за нуль днів");

        BookCopy copy = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        copy.Issue(); // якщо інваріант кине виняток — події не буде
        _store.Update(copy);

        DateOnly dueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(loanDays));
        OnChanged(copy, LendingChangeKind.CopyIssued, readerId.Trim(), dueDate);
    }

    public void ReturnCopy(string id)
    {
        BookCopy copy = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        copy.Return();
        _store.Update(copy);
        OnChanged(copy, LendingChangeKind.CopyReturned);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);

    // Func замість події: тут потрібне ЗНАЧЕННЯ у відповідь, критерій задає той, хто викликає.
    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate) =>
        _store.List().Where(predicate).ToList();
}
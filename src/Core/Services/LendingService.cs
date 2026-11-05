using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService(IBookStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly IBookStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly List<INotificationSink> _sinks = sinks?.ToList() ?? [];

    public event EventHandler<LendingChangedEventArgs>? Changed;

    private void OnChanged(
        BookCopy copy, LendingChangeKind kind, string? readerId = null, DateOnly? dueDate = null)
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
        OnChanged(copy, LendingChangeKind.BookAdded);
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
            ?? throw new DomainException($"Немає запису з id={id}.");

        copy.Issue();
        _store.Update(copy);

        DateOnly dueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(loanDays));
        OnChanged(copy, LendingChangeKind.CopyIssued, readerId.Trim(), dueDate);
    }

    public void ReturnCopy(string id)
    {
        BookCopy copy = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");

        copy.Return();
        _store.Update(copy);
        OnChanged(copy, LendingChangeKind.CopyReturned);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);

    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate) =>
        _store.List().Where(predicate).ToList();

    // ---------------- Лабораторна 8: асинхронний імпорт ----------------

    // progress отримує ВІДСОТКИ (0..100). Імпорт ідемпотентний: наявні id пропускаються.
    public async Task<int> ImportAsync(
        ICatalogImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<BookCopy> items = await importer.LoadAsync(path, ct);
        int added = 0, processed = 0;

        foreach (BookCopy item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item, LendingChangeKind.BookAdded);
                added++;
            }
            processed++;
            progress?.Report(processed * 100 / items.Count);
        }
        return added;
    }

    // Потоковий імпорт (дод. завдання 1). Загальна кількість невідома,
    // тому progress отримує кількість ОБРОБЛЕНИХ записів.
    public async Task<int> ImportStreamAsync(
        ICatalogImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        int added = 0, processed = 0;

        try
        {
            await foreach (BookCopy item in importer.StreamAsync(path, ct).WithCancellation(ct))
            {
                if (_store.GetById(item.Id) is null)
                {
                    _store.Add(item);
                    OnChanged(item, LendingChangeKind.BookAdded);
                    added++;
                }
                progress?.Report(++processed);
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} не є коректним JSON", ex);
        }
        return added;
    }

    // Зв'язані токени (дод. завдання 2): користувацьке скасування + таймаут.
    // Таймаут відрізняється від Ctrl+C: кидається TimeoutException.
    public async Task<int> ImportWithTimeoutAsync(
        ICatalogImporter importer, string path, TimeSpan timeout,
        CancellationToken userToken = default, IProgress<int>? progress = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutCts.Token);
        try
        {
            return await ImportAsync(importer, path, linked.Token, progress);
        }
        catch (OperationCanceledException)
            when (timeoutCts.IsCancellationRequested && !userToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Імпорт перевищив ліміт {timeout.TotalSeconds:F1} с");
        }
    }
}
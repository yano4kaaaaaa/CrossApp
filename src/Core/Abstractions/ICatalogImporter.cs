using Core.Domain;

namespace Core.Abstractions;

public interface ICatalogImporter
{
    // Завантажує все одразу (атомарно: поганий запис — нічого не імпортується).
    Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default);

    // Потоковий варіант (додаткове завдання 1).
    IAsyncEnumerable<BookCopy> StreamAsync(string path, CancellationToken ct = default);
}
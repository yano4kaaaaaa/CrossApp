using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Файлове сховище примірників: шлях приходить ззовні (з конфігурації), запис атомарний.
// Контракт IBookStore лишається синхронним (як у тижнях 5-8).
public sealed class FileBookStore : IBookStore
{
    private readonly string _path;
    private readonly Dictionary<string, BookCopy> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public FileBookStore(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public IReadOnlyList<BookCopy> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public BookCopy? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();

        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        bool removed = _cache.Remove(id);
        if (removed)
            Flush();
        return removed;
    }

    private void Flush() =>
        JsonDefaults.WriteAtomic(_path, _cache.Values.Select(c => c.ToDto()).ToList());

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            List<BookCopyDto> dtos;
            try
            {
                dtos = JsonSerializer.Deserialize<List<BookCopyDto>>(
                    File.ReadAllText(_path), JsonDefaults.Options) ?? [];
            }
            catch (JsonException ex)
            {
                throw new StorageException(
                    $"Файл {Path.GetFileName(_path)} не є коректним JSON: {ex.Message}", ex);
            }

            _cache.Clear();
            for (int i = 0; i < dtos.Count; i++)
            {
                if (dtos[i] is null)
                    throw new DomainException($"Запис {i + 1}: порожнє значення");

                try
                {
                    BookCopy copy = BookCopy.FromDto(dtos[i]);   // повторно перевіряє інваріанти
                    _cache[copy.Id] = copy;
                }
                catch (ArgumentException ex)
                {
                    throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
                }
            }
        }

        _loaded = true;
    }
}
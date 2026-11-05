using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // useAsync: true — справжній асинхронний ввід-вивід, без Task.Run.
    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

    public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        string name = Path.GetFileName(path);

        List<BookCopyDto> dtos;
        try
        {
            await using FileStream stream = Open(path);
            dtos = await JsonSerializer.DeserializeAsync<List<BookCopyDto>>(stream, Options, ct)
                ?? throw new DomainException($"Файл {name} не містить списку.");
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {name} не є коректним JSON", ex);
        }

        var result = new List<BookCopy>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(Map(dtos[i], i + 1));
        }
        return result;
    }

    public async IAsyncEnumerable<BookCopy> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using FileStream stream = Open(path);

        int i = 0;
        await foreach (BookCopyDto? dto in
            JsonSerializer.DeserializeAsyncEnumerable<BookCopyDto>(stream, Options, ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return Map(dto, ++i);
        }
    }

    private static BookCopy Map(BookCopyDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожнє значення");
        try { return BookCopy.FromDto(dto); }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }
}
using Core;
using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public class BookCatalogTests
{
    private static Book B(string id, string isbn, string title, string author, int year = 2000, int copies = 1) =>
        Book.Create(id, isbn, title, author, year, copies);

    [Fact]
    public void Book_Create_EmptyTitle_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => B("1", "111", " ", "Автор"));

    [Fact]
    public void Book_Create_NegativeCopies_ThrowsArgumentOutOfRange() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => B("1", "111", "Назва", "Автор", copies: -1));

    [Fact]
    public async Task AddAsync_DuplicateIsbn_ThrowsDomainException()
    {
        var catalog = new InMemoryBookCatalog();
        await catalog.AddAsync(B("1", "111", "Кобзар", "Шевченко"));

        await Assert.ThrowsAsync<DomainException>(
            () => catalog.AddAsync(B("2", "111", "Інша", "Інший")));
    }

    [Fact]
    public async Task SearchAsync_FiltersByAuthor_AndPages()
    {
        var catalog = new InMemoryBookCatalog(
        [
            B("1", "i1", "А книга", "Тарас Шевченко"),
            B("2", "i2", "Б книга", "Тарас Шевченко"),
            B("3", "i3", "В книга", "Тарас Шевченко"),
            B("4", "i4", "Г книга", "Леся Українка")
        ]);

        var page = await catalog.SearchAsync("шевч", null, skip: 2, take: 2);

        Assert.Equal(3, page.Total);
        Assert.Equal(new[] { "В книга" }, page.Items.Select(b => b.Title));
    }

    [Fact]
    public async Task AddAsync_Parallel_LosesNoRecords()
    {
        var catalog = new InMemoryBookCatalog();

        await Task.WhenAll(Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => catalog.AddAsync(B($"id{i}", $"isbn{i}", $"Назва {i}", "Автор")))));

        Assert.Equal(100, (await catalog.SearchAsync(null, null, 0, 1000)).Total);
    }

    [Fact]
    public async Task RemoveAsync_Unknown_ReturnsFalse() =>
        Assert.False(await new InMemoryBookCatalog().RemoveAsync("no-such"));

    [Fact]
    public async Task SearchAsync_WhenCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new InMemoryBookCatalog().SearchAsync(null, null, 0, 10, cts.Token));
    }
}
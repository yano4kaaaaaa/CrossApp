using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    private sealed class ListProgress : IProgress<int> // синхронний, на відміну від Progress<T>
    {
        public List<int> Values { get; } = [];
        public void Report(int v) => Values.Add(v);
    }

    // Імпортер, що «висить» до скасування: для тесту таймауту.
    private sealed class HangingImporter : ICatalogImporter
    {
        public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct);
            return [];
        }
        public IAsyncEnumerable<BookCopy> StreamAsync(string path, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private static async Task WithTempFile(string json, Func<string, Task> body)
    {
        string path = Path.Combine(Path.GetTempPath(), $"lab08-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        try { await body(path); }
        finally { File.Delete(path); }
    }

    private const string Good =
        """[{"id":"A-1","isbn":"111"},{"id":"A-2","isbn":"222"},{"id":"A-3","isbn":"333"}]""";

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new LendingService(new InMemoryBookStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact]
    public Task ImportAsync_BadRecord_ThrowsDomainException_WithRecordNumber() =>
        WithTempFile("""[{"id":"A-1","isbn":"111"},{"id":"A-2","isbn":""}]""", async path =>
        {
            var store = new InMemoryBookStore();
            var service = new LendingService(store);

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonCatalogImporter(), path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
            Assert.Empty(store.List()); // атомарність: нічого не додано
        });

    [Fact]
    public Task ImportAsync_AddsAll_RaisesEventPerItem_AndIsIdempotent() =>
        WithTempFile(Good, async path =>
        {
            var service = new LendingService(new InMemoryBookStore());
            var events = new List<LendingChangedEventArgs>();
            service.Changed += (_, e) => events.Add(e);
            var importer = new JsonCatalogImporter();

            Assert.Equal(3, await service.ImportAsync(importer, path));
            Assert.Equal(0, await service.ImportAsync(importer, path));
            Assert.Equal(3, events.Count);
        });

    [Fact]
    public Task ImportAsync_BrokenJson_ThrowsStorageException_WithInner() =>
        WithTempFile("{ не json", async path =>
        {
            var service = new LendingService(new InMemoryBookStore());
            var ex = await Assert.ThrowsAsync<StorageException>(
                () => service.ImportAsync(new JsonCatalogImporter(), path));
            Assert.NotNull(ex.InnerException);
        });

    [Fact] // дод. 1
    public Task ImportStreamAsync_ImportsAll_AndReportsCount() =>
        WithTempFile(Good, async path =>
        {
            var service = new LendingService(new InMemoryBookStore());
            var progress = new ListProgress();

            int added = await service.ImportStreamAsync(new JsonCatalogImporter(), path, progress: progress);

            Assert.Equal(3, added);
            Assert.Equal(new[] { 1, 2, 3 }, progress.Values);
        });

    [Fact] // дод. 1: скасування посеред потоку
    public Task ImportStreamAsync_CancelledMidStream_Throws() =>
        WithTempFile(Good, async path =>
        {
            using var cts = new CancellationTokenSource();
            var service = new LendingService(new InMemoryBookStore());
            service.Changed += (_, _) => cts.Cancel(); // скасовуємо після першого запису

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.ImportStreamAsync(new JsonCatalogImporter(), path, cts.Token));
        });

    [Fact] // дод. 2: спрацював саме таймаут, а не користувач
    public async Task ImportWithTimeoutAsync_Timeout_ThrowsTimeoutException()
    {
        using var user = new CancellationTokenSource();
        var service = new LendingService(new InMemoryBookStore());

        await Assert.ThrowsAsync<TimeoutException>(() =>
            service.ImportWithTimeoutAsync(new HangingImporter(), "x", TimeSpan.FromMilliseconds(50), user.Token));

        Assert.False(user.IsCancellationRequested);
    }

    [Fact] // дод. 2: скасування користувачем не маскується під таймаут
    public async Task ImportWithTimeoutAsync_UserCancel_ThrowsOperationCanceled()
    {
        using var user = new CancellationTokenSource();
        var service = new LendingService(new InMemoryBookStore());
        user.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportWithTimeoutAsync(new HangingImporter(), "x", TimeSpan.FromSeconds(30), user.Token));
    }

    [Fact] // дод. 3
    public Task ImportAsync_ReportsPercents_EndingAt100() =>
        WithTempFile(Good, async path =>
        {
            var progress = new ListProgress();
            await new LendingService(new InMemoryBookStore())
                .ImportAsync(new JsonCatalogImporter(), path, progress: progress);

            Assert.Equal(new[] { 33, 66, 100 }, progress.Values);
        });
}
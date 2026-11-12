using System.Text.Json;
using Core;
using Core.Configuration;
using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public class FileStoreTests
{
    private static void WithTempDir(Action<string> body)
    {
        string dir = Path.Combine(Path.GetTempPath(), "lab09-" + Guid.NewGuid().ToString("N"));
        try { body(dir); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ResolveFullPath_Relative_UsesBaseDir_AndCreatesFolder() =>
        WithTempDir(dir =>
        {
            var options = new StorageOptions { DataPath = "data", FileName = "x.json" };

            string full = options.ResolveFullPath(dir);

            Assert.Equal(Path.GetFullPath(Path.Combine(dir, "data", "x.json")), full);
            Assert.True(Directory.Exists(Path.Combine(dir, "data")));
        });

    [Fact]
    public void ResolveFullPath_Absolute_IgnoresBaseDir() =>
        WithTempDir(dir =>
        {
            string abs = Path.Combine(dir, "abs");
            var options = new StorageOptions { DataPath = abs, FileName = "x.json" };

            string full = options.ResolveFullPath(Path.Combine(dir, "other"));

            Assert.Equal(Path.GetFullPath(Path.Combine(abs, "x.json")), full);
        });

    [Fact]
    public void Store_Persists_BetweenInstances_AndCreatesMissingFolder() =>
        WithTempDir(dir =>
        {
            string path = Path.Combine(dir, "sub", "lib.json");   // папки sub ще немає
            var first = new FileBookStore(path);
            first.Add(BookCopy.Create("C-1", "111"));
            first.Add(BookCopy.Create("C-2", "222", isIssued: true));

            var second = new FileBookStore(path);

            Assert.Equal(2, second.List().Count);
            Assert.True(second.GetById("C-2")!.IsIssued);
        });

    [Fact]
    public void Store_Flush_LeavesNoTmpFile() =>
        WithTempDir(dir =>
        {
            string path = Path.Combine(dir, "lib.json");

            new FileBookStore(path).Add(BookCopy.Create("C-1", "111"));

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));
        });

    [Fact]
    public void Store_BrokenJson_ThrowsStorageException() =>
        WithTempDir(dir =>
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "lib.json");
            File.WriteAllText(path, "{ broken");

            Assert.Throws<StorageException>(() => new FileBookStore(path).List());
        });

    [Fact]
    public void Store_BadRecord_ThrowsDomainException_WithRecordNumber() =>
        WithTempDir(dir =>
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "lib.json");
            File.WriteAllText(path, """[{"id":"A","isbn":""}]""");

            var ex = Assert.Throws<DomainException>(() => new FileBookStore(path).List());

            Assert.Contains("Запис 1", ex.Message);
        });

    [Fact]
    public void JsonDefaults_DoesNotEscapeCyrillic()
    {
        string json = JsonSerializer.Serialize(new { title = "Цемент" }, JsonDefaults.Options);

        Assert.Contains("Цемент", json);
        Assert.DoesNotContain("\\u0426", json);
    }

    [Fact]
    public void WriteAtomic_OverwritesExistingFile() =>
        WithTempDir(dir =>
        {
            string path = Path.Combine(dir, "a.json");

            JsonDefaults.WriteAtomic(path, new[] { 1 });
            JsonDefaults.WriteAtomic(path, new[] { 2 });

            Assert.Contains("2", File.ReadAllText(path));
            Assert.DoesNotContain("1", File.ReadAllText(path));
        });
}
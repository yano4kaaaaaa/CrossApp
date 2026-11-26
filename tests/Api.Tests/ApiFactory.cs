using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const string Variable = "ConnectionStrings__AppDb";
    private readonly string _dbPath;
    private readonly bool _ownsFile;
    private readonly string? _previous;

    public ApiFactory(string? dbPath = null)
    {
        _ownsFile = dbPath is null;
        _dbPath = dbPath ?? NewTempDbPath();
        _previous = Environment.GetEnvironmentVariable(Variable);
        Environment.SetEnvironmentVariable(Variable, $"Data Source={_dbPath}");
    }

    public static string NewTempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"lab11-{Guid.NewGuid():N}.db");

    public static void Cleanup(string path)
    {
        SqliteConnection.ClearAllPools();
        foreach (string file in new[] { path, path + "-wal", path + "-shm", path + "-journal" })
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable(Variable, _previous);
        if (_ownsFile)
            Cleanup(_dbPath);
    }
}
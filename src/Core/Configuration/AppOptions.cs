namespace Core.Configuration;

// Налаштування застосунку. Біндяться з IConfiguration через config.Get<AppOptions>() у Cli.
// Core про JSON-файли, user-secrets і змінні середовища нічого не знає: лише класи з властивостями.
public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public LibraryOptions Library { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "library.json";

    // Відносний DataPath рахується від baseDir (а не від поточного робочого каталогу),
    // тому запуск з будь-якої папки дає той самий файл.
    public string ResolveFullPath(string baseDir)
    {
        string root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

public sealed class LibraryOptions
{
    public int LoanDays { get; init; } = 14;   // ключ Library:LoanDays (варіант «Бібліотека»)
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}
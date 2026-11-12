using System.Text.Encodings.Web;
using System.Text.Json;

namespace Core.Storage;

// Єдині налаштування JSON для всього проєкту + атомарний запис файлу.
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // Кирилиця у файлі лишається читабельною, а не \u0426.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Атомарний запис: спочатку .tmp, потім File.Move з перезаписом.
    // Якщо процес впаде посеред запису, старий файл лишиться цілим.
    public static void WriteAtomic<T>(string path, T value)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, Options));
        File.Move(tmp, path, overwrite: true);
    }
}
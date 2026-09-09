using System.Runtime.InteropServices;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var info = new
{
    Student = "Столярська Яна, група ФЕІ-34",
    OsDescription = RuntimeInformation.OSDescription,
    OsEnvironment = Environment.OSVersion.ToString(),
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Бібліотека (книги, примірники, читачі, видачі)"
};

if (args.Contains("--json"))
{
    // Додаткове завдання 2: якщо передано --json — вивести один JSON-рядок
    string json = JsonSerializer.Serialize(info);
    Console.WriteLine(json);
}
else
{
    // Звичайний вивід таблицею (як і раніше)
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine($"Студент: {info.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)     : {info.OsDescription}");
    Console.WriteLine($"ОС (Environment)       : {info.OsEnvironment}");
    Console.WriteLine($"Архітектура процесу    : {info.Architecture}");
    Console.WriteLine($"Версія .NET (CLR)      : {info.DotNetVersion}");
    Console.WriteLine($"Runtime                : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку     : {info.AppDirectory}");
    Console.WriteLine($"Поточний каталог       : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.Domain}");
}
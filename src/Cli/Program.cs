using Cli.Notifications;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

// --- Composition root ---
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

IBookStore store = StoreFactory.Create(args, dataPath);
var service = new LendingService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine($"Лог: {logPath}");
Console.WriteLine();

// Підписник 1: ЛОКАЛЬНИЙ МЕТОД (щоб згодом можна було відписатися через -=).
void OnLendingChanged(object? sender, LendingChangedEventArgs e)
{
    string line = EventFormatter.Format(e);
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}
service.Changed += OnLendingChanged;

// Підписник 2: ЛЯМБДА із замиканням над локальною змінною changes.
int changes = 0;
service.Changed += (_, _) =>
{
    changes++;
    Console.WriteLine($"  [лічильник] подія №{changes}");
};

Console.WriteLine("=== Сценарій 1: успіх (події виникають) ===");
BookCopy created = service.AddBook("978-1-999999-9-9");
service.IssueCopy(created.Id, "R-100");
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: відмова (виняток є, події немає) ===");
int before = changes;
TryDo("видача неіснуючого id", () => service.IssueCopy("NO-SUCH-ID", "R-1"));
TryDo("порожній читач", () => service.IssueCopy(created.Id, " "));
TryDo("повторна видача виданого примірника", () => service.IssueCopy(created.Id, "R-2"));
Console.WriteLine($"Нових подій під час відмов: {changes - before}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 3: відписка -= ===");
service.Changed -= OnLendingChanged;
service.ReturnCopy(created.Id); // тиха операція: у консолі й лозі рядка події немає
Console.WriteLine($"Подій отримано: {changes}"); // +1 лише від лямбди-лічильника

Console.WriteLine();
Console.WriteLine("=== Func замість події: FindAll з предикатом ===");
IReadOnlyList<BookCopy> available = service.FindAll(c => !c.IsIssued);
Console.WriteLine($"Доступні примірники: {available.Count}");
IReadOnlyList<BookCopy> issued = service.FindAll(c => c.IsIssued);
Console.WriteLine($"Видані примірники: {issued.Count}");

Console.WriteLine();
Console.WriteLine("=== Додаткове 1: INotificationSink замість події ===");
string sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
var sinkService = new LendingService(
    new InMemoryBookStore(),
    [new ConsoleSink(), new FileSink(sinkLogPath)]);

BookCopy sinkCopy = sinkService.AddBook("978-2-222222-2-2");
sinkService.IssueCopy(sinkCopy.Id, "R-200", loanDays: 7);

Console.WriteLine();
Console.WriteLine("=== Додаткове 2: Action<string> у методі імпорту ===");
string samplePath = Path.Combine("data", "sample.csv");
if (File.Exists(samplePath))
{
    var imported = BookCsvImporter.Load(samplePath, msg => Console.WriteLine($"  [пропущено] {msg}"));
    Console.WriteLine($"Завантажено: {imported.Items.Count}, пропущено: {imported.Errors.Count}");
}
else
{
    Console.WriteLine($"  файл {Path.GetFullPath(samplePath)} не знайдено — запускайте з кореня CrossApp");
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core;

// --- Composition root: єдине місце, де вибирається конкретна реалізація сховища ---
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");
IBookStore store = StoreFactory.Create(args, dataPath);
var service = new LendingService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine();

Console.WriteLine("=== Сценарій: успіх ===");

BookCopy created = service.AddBook("978-1-999999-9-9");
Console.WriteLine($"Додано: {created}");

service.IssueCopy(created.Id);
Console.WriteLine($"Видано: {service.Find(created.Id)}");

Console.WriteLine();
Console.WriteLine("Усі записи сховища:");
foreach (BookCopy c in service.All())
    Console.WriteLine($"  {c}");

// Додаткове завдання 2: пошук за довільним предикатом.
Console.WriteLine();
IReadOnlyList<BookCopy> issuedOnly = service.Find(c => c.IsIssued);
Console.WriteLine($"Видані примірники (Find з Func<BookCopy,bool>): {issuedOnly.Count}");
foreach (BookCopy c in issuedOnly)
    Console.WriteLine($"  {c}");

Console.WriteLine();
Console.WriteLine("=== Сценарій: відмова ===");

TryDo("видача неіснуючого id", () => service.IssueCopy("NO-SUCH-ID"));
TryDo("повторна видача вже виданого примірника", () => service.IssueCopy(created.Id));

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
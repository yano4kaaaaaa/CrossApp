using Core.Domain;
using Core.Dto;
using Core.Import;

Console.WriteLine("=== Сценарій 1: успіх ===");

BookCopy copy = BookCopy.Create("C-001", "978-966-03-5219-4");
Console.WriteLine(copy);

Loan loan = Loan.Open(copy, "R-100", new DateTime(2026, 1, 10));
Console.WriteLine(copy);
Console.WriteLine(loan);

loan.Close(new DateTime(2026, 1, 20));
copy.Return();
Console.WriteLine(copy);
Console.WriteLine(loan);

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

TryDo("повторна видача вже виданого примірника", () =>
{
    BookCopy c = BookCopy.Create("C-002", "978-0-13-468599-1");
    c.Issue();
    c.Issue();
});

TryDo("порожній ISBN примірника", () => BookCopy.Create("C-003", " "));

TryDo("дата повернення раніше дати видачі", () =>
{
    BookCopy c = BookCopy.Create("C-004", "978-1-234567-89-0");
    Loan l = Loan.Open(c, "R-5", new DateTime(2026, 1, 10));
    l.Close(new DateTime(2026, 1, 1));
});

TryDo("повторне закриття видачі", () =>
{
    BookCopy c = BookCopy.Create("C-005", "978-9-87654-32-1");
    Loan l = Loan.Open(c, "R-6", new DateTime(2026, 1, 1));
    l.Close(new DateTime(2026, 1, 5));
    l.Close(new DateTime(2026, 1, 6));
});

TryDo("порожній ідентифікатор читача", () =>
{
    BookCopy c = BookCopy.Create("C-006", "978-0-000000-0-0");
    Loan.Open(c, " ", DateTime.Now);
});

Console.WriteLine();
Console.WriteLine("=== Додаткове завдання 1: ImportResult -> сутності + помилки інваріантів ===");

ImportResult<Core.Dto.BookCopyDto> importResult = BookCopyCsvImporter.Load(Path.Combine("data", "copies.csv"));
(IReadOnlyList<BookCopy> copies, IReadOnlyList<string> entityErrors) = BookCopyImportAdapter.ToEntities(importResult);

Console.WriteLine($"Створено сутностей: {copies.Count}");
foreach (BookCopy c in copies)
    Console.WriteLine($"  {c}");

if (entityErrors.Count > 0)
{
    Console.WriteLine($"Відхилено рядків: {entityErrors.Count}");
    foreach (string e in entityErrors)
        Console.WriteLine($"  ! {e}");
}

Console.WriteLine();
Console.WriteLine("=== Додаткове завдання 2: інваріант на дві сутності (ліміт відкритих видач) ===");

var existingLoans = new List<Loan>();
BookCopy policyCopy1 = BookCopy.Create("C-201", "978-1-111111-1-1");
existingLoans.Add(Loan.Open(policyCopy1, "R-900", new DateTime(2026, 1, 1)));
BookCopy policyCopy2 = BookCopy.Create("C-202", "978-1-222222-2-2");
existingLoans.Add(Loan.Open(policyCopy2, "R-900", new DateTime(2026, 1, 2)));
BookCopy policyCopy3 = BookCopy.Create("C-203", "978-1-333333-3-3");
existingLoans.Add(Loan.Open(policyCopy3, "R-900", new DateTime(2026, 1, 3)));
BookCopy policyCopy4 = BookCopy.Create("C-204", "978-1-444444-4-4");
existingLoans.Add(Loan.Open(policyCopy4, "R-900", new DateTime(2026, 1, 4)));
BookCopy policyCopy5 = BookCopy.Create("C-205", "978-1-555555-5-5");
existingLoans.Add(Loan.Open(policyCopy5, "R-900", new DateTime(2026, 1, 5)));

Console.WriteLine($"У читача R-900 відкритих видач: {existingLoans.Count}");

TryDo("6-та видача тому самому читачу (ліміт 5)", () =>
{
    LoanPolicy.EnsureReaderCanBorrow("R-900", existingLoans);
    BookCopy policyCopy6 = BookCopy.Create("C-206", "978-1-666666-6-6");
    Loan.Open(policyCopy6, "R-900", new DateTime(2026, 1, 6));
});

Console.WriteLine();
Console.WriteLine("=== Додаткове завдання 3: явний стан LoanStatus і перевірка переходів ===");

BookCopy statusCopy = BookCopy.Create("C-301", "978-1-777777-7-7");
Loan statusLoan = Loan.Open(statusCopy, "R-950", new DateTime(2026, 1, 1));
Console.WriteLine($"Стан після відкриття: {statusLoan.Status}");
statusLoan.Close(new DateTime(2026, 1, 10));
Console.WriteLine($"Стан після закриття: {statusLoan.Status}");

TryDo("перехід Closed -> Closed (повторне закриття)", () => statusLoan.Close(new DateTime(2026, 1, 15)));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}
using System.Diagnostics;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Reports;
using Core.Services;

// --- Composition root ---
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");
IBookStore store = StoreFactory.Create(args, dataPath);
var service = new LendingService(store);

DateTime today = DateTime.Today;
IReadOnlyList<BookCopy> copies = service.All();
IReadOnlyList<Loan> loans = SampleData.Loans(today);
IReadOnlyList<BookDto> books = SampleData.Books();

// Прапорці (--file, --cache) не вважаються назвою звіту.
string command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

switch (command)
{
    case "report-top":     PrintTop(loans); break;
    case "report-overdue": PrintOverdue(loans, today); break;
    case "report-books":   PrintBooks(books); break;
    case "report-stats":   PrintStats(loans); break;
    case "report-newest":  PrintNewest(books); break;
    case "report-longest": PrintLongest(loans); break;
    case "report-details": PrintDetails(loans, copies, books); break;
    case "report-query":   PrintQuerySyntax(books); break;
    case "report-stable":  PrintStable(loans); break;
    case "report-empty":   PrintEmpty(); break;
    case "report-defer":   PrintDefer(copies); break;
    case "report-bench":   PrintBench(today); break;

    case "report-all":
        PrintTop(loans);
        PrintOverdue(loans, today);
        PrintBooks(books);
        PrintStats(loans);
        PrintNewest(books);
        PrintLongest(loans);
        PrintDetails(loans, copies, books);
        PrintQuerySyntax(books);
        PrintStable(loans);
        PrintEmpty();
        PrintDefer(copies);
        break;

    default:
        Console.WriteLine("Доступні команди:");
        Console.WriteLine("  report-top  report-overdue  report-books  report-stats");
        Console.WriteLine("  report-newest  report-longest  report-details  report-query");
        Console.WriteLine("  report-stable  report-empty  report-defer  report-bench  report-all");
        break;
}

// ---------------------------------------------------------------------------
// Лише виведення. Уся логіка запитів — у Core/Reports.
// ---------------------------------------------------------------------------

static void PrintTop(IReadOnlyList<Loan> loans)
{
    Console.WriteLine("Топ-5 читачів за кількістю видач:");
    foreach (ReaderLoanCount r in ReportService.TopReaders(loans, 5))
        Console.WriteLine($"  {r.ReaderId,-8} видач: {r.Loans,3}");
}

static void PrintOverdue(IReadOnlyList<Loan> loans, DateTime today)
{
    Console.WriteLine($"Прострочені видачі (термін 14 днів, станом на {today:dd.MM.yyyy}):");
    IReadOnlyList<OverdueLoanLine> lines = ReportService.OverdueLoans(loans, today);

    if (lines.Count == 0)
    {
        Console.WriteLine("  немає");
        return;
    }

    foreach (OverdueLoanLine l in lines)
        Console.WriteLine(
            $"  {l.LoanId,-7} {l.CopyId,-6} {l.ReaderId,-6} видано {l.IssuedOn:dd.MM.yyyy}, " +
            $"до {l.DueOn:dd.MM.yyyy}, прострочено на {l.DaysOverdue,3} дн.");
}

static void PrintBooks(IReadOnlyList<BookDto> books)
{
    Console.WriteLine("Видання за століттями:");
    foreach (BookGroup g in ReportService.BooksByCentury(books))
        Console.WriteLine($"  {g.Century,-7} назв: {g.Titles,3}   роки {g.OldestYear}–{g.NewestYear}");
}

static void PrintStats(IReadOnlyList<Loan> loans)
{
    LoanStats s = ReportService.LoanStatistics(loans);
    Console.WriteLine("Статистика завершених видач:");
    Console.WriteLine(
        $"  завершених {s.Closed}, всього днів {s.TotalDays}, " +
        $"середня тривалість {s.AverageDays:F2} дн., найдовша {s.MaxDays} дн.");
}

static void PrintNewest(IReadOnlyList<BookDto> books)
{
    Console.WriteLine("Топ-5 найновіших видань (TopBy):");
    foreach (BookLine b in ReportService.NewestBooks(books, 5))
        Console.WriteLine($"  {b.Id,-6} {b.Title,-30} {b.Year,5}  {b.Author ?? "—"}");
}

static void PrintLongest(IReadOnlyList<Loan> loans)
{
    Console.WriteLine("Топ-5 найдовших завершених видач (TopBy):");
    foreach (LoanLengthLine l in ReportService.LongestLoans(loans, 5))
        Console.WriteLine($"  {l.LoanId,-7} {l.ReaderId,-6} {l.Days,3} дн.");
}

static void PrintDetails(
    IReadOnlyList<Loan> loans, IReadOnlyList<BookCopy> copies, IReadOnlyList<BookDto> books)
{
    IReadOnlyList<LoanDetailLine> details = ReportService.LoanDetails(loans, copies, books);
    Console.WriteLine($"Видачі з назвами книг (Join), усього {details.Count}, перші 8:");
    foreach (LoanDetailLine d in details.Take(8))
        Console.WriteLine(
            $"  {d.LoanId,-7} {d.ReaderId,-6} {d.Title,-32} {d.IssuedOn:dd.MM.yyyy} " +
            $"{(d.IsOpen ? "відкрита" : "закрита")}");

    if (details.Count == 0)
        Console.WriteLine("  (сховище порожнє — запускайте без --file)");
}

static void PrintQuerySyntax(IReadOnlyList<BookDto> books)
{
    Console.WriteLine("Видання за століттями — синтаксис запитів (from ... group ... into):");
    foreach (BookGroup g in ReportService.BooksByCenturyQuerySyntax(books))
        Console.WriteLine($"  {g.Century,-7} назв: {g.Titles,3}   роки {g.OldestYear}–{g.NewestYear}");

    Console.WriteLine(
        $"Методний синтаксис і синтаксис запитів дають однаковий результат: " +
        $"{ReportService.CenturySyntaxesAgree(books)}");
}

static void PrintStable(IReadOnlyList<Loan> loans)
{
    Console.WriteLine(
        $"Порядок топ-5 не залежить від порядку джерела: " +
        $"{ReportService.TopReadersIsOrderIndependent(loans, 5)}");
}

static void PrintEmpty()
{
    Console.WriteLine("Звіти на порожньому джерелі (без винятків):");

    Loan[] noLoans = [];
    BookDto[] noBooks = [];
    LoanStats stats = ReportService.LoanStatistics(noLoans);

    Console.WriteLine($"  TopReaders:     {ReportService.TopReaders(noLoans, 5).Count} рядків");
    Console.WriteLine($"  OverdueLoans:   {ReportService.OverdueLoans(noLoans, DateTime.Today).Count} рядків");
    Console.WriteLine($"  BooksByCentury: {ReportService.BooksByCentury(noBooks).Count} рядків");
    Console.WriteLine($"  NewestBooks:    {ReportService.NewestBooks(noBooks, 5).Count} рядків");
    Console.WriteLine(
        $"  LoanStatistics: закритих {stats.Closed}, середнє {stats.AverageDays:F2}, макс {stats.MaxDays}");
}

static void PrintDefer(IReadOnlyList<BookCopy> copies)
{
    DeferredDemoResult r = DeferredExecutionDemo.Run(copies);

    Console.WriteLine("Відкладене виконання:");
    Console.WriteLine($"  Виданих примірників до змін джерела:       {r.Before}");
    Console.WriteLine($"  Виданих примірників після додавання в нього: {r.After}");
    Console.WriteLine($"  У знімку (ToList) лишилось:                {r.SnapshotCount}");
    Console.WriteLine($"  Після Clear() запит бачить:                {r.QueryAfterClear}, а знімок: {r.SnapshotCount}");
}

static void PrintBench(DateTime today)
{
    List<Loan> big = LoanGenerator.Generate(100_000, today);
    Console.WriteLine($"Згенеровано видач: {big.Count}");
    Console.WriteLine("Середній час одного виконання (30 запусків, мс):");

    TimeIt("A: без проміжного ToList (2 проходи)", () => ReportBenchmarks.WithoutMaterialization(big));
    TimeIt("B: один ToList після Where", () => ReportBenchmarks.MaterializeOnce(big));
    TimeIt("C: ToList після кожного кроку", () => ReportBenchmarks.MaterializeEverywhere(big));
}

// Func<BenchResult> — приклад Func із лабораторної 6: вимірюваний код передається як значення.
static void TimeIt(string name, Func<BenchResult> action, int runs = 30)
{
    BenchResult last = action();   // прогрів (JIT), у вимір не входить

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < runs; i++)
        last = action();
    sw.Stop();

    Console.WriteLine(
        $"  {name,-40} {sw.Elapsed.TotalMilliseconds / runs,8:F2} мс   " +
        $"(відкритих {last.OpenLoans}, у топі {last.TopReaders})");
}
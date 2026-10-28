using Core.Domain;
using Core.Dto;

namespace Core.Reports;

// Чисті функції: приймають IEnumerable<T> (не сховище), повертають IReadOnlyList<DTO>.
// Жодного Console і жодного звернення до файлів.
public static class ReportService
{
    // Допоміжні обчислення (щоб не дублювати вирази в запитах).
    private static DateTime DueOn(Loan l, int loanDays) => l.IssuedOn.AddDays(loanDays);

    private static int DaysOverdue(Loan l, DateTime asOf, int loanDays) =>
        (asOf.Date - DueOn(l, loanDays).Date).Days;

    private static int Duration(Loan l) => (l.ReturnedOn!.Value - l.IssuedOn).Days;

    // 1) Фільтр + сортування + проєкція: які видачі прострочені?
    //    Where -> OrderByDescending -> ThenBy -> Select -> ToList
    public static IReadOnlyList<OverdueLoanLine> OverdueLoans(
        IEnumerable<Loan> loans, DateTime asOf, int loanDays = 14) =>
        loans.Where(l => !l.IsClosed && DueOn(l, loanDays) < asOf)
             .OrderByDescending(l => DaysOverdue(l, asOf, loanDays))
             .ThenBy(l => l.Id, StringComparer.Ordinal)
             .Select(l => new OverdueLoanLine(
                 l.Id, l.CopyId, l.ReaderId, l.IssuedOn, DueOn(l, loanDays),
                 DaysOverdue(l, asOf, loanDays)))
             .ToList();

    // 2) Топ-N зі стабільним порядком: які читачі беруть книги найчастіше?
    //    GroupBy -> Select -> OrderByDescending -> ThenBy (унікальний ключ) -> Take
    public static IReadOnlyList<ReaderLoanCount> TopReaders(IEnumerable<Loan> loans, int n) =>
        loans.GroupBy(l => l.ReaderId, StringComparer.OrdinalIgnoreCase)
             .Select(g => new ReaderLoanCount(g.Key, g.Count()))
             .OrderByDescending(r => r.Loans)
             .ThenBy(r => r.ReaderId, StringComparer.Ordinal)
             .Take(n)
             .ToList();

    // 3) Групування з проєкцією: як розподілені видання за століттями?
    //    g.Key — століття (ключ групи), g.Count() — кількість назв у групі,
    //    g.Min / g.Max — найстаріший і найновіший рік у групі.
    public static IReadOnlyList<BookGroup> BooksByCentury(IEnumerable<BookDto> books) =>
        books.GroupBy(b => (b.Year - 1) / 100 + 1)
             .OrderBy(g => g.Key)
             .Select(g => new BookGroup(
                 $"{g.Key} ст.", g.Count(), g.Min(b => b.Year), g.Max(b => b.Year)))
             .ToList();

    // 4) Агрегати з обробкою порожньої послідовності: яка тривалість завершених видач?
    //    Для порожнього джерела Average і Max кинули б InvalidOperationException,
    //    тому спершу перевіряємо Count.
    public static LoanStats LoanStatistics(IEnumerable<Loan> loans)
    {
        List<int> durations = loans.Where(l => l.IsClosed)
                                   .Select(Duration)
                                   .ToList();

        return durations.Count == 0
            ? new LoanStats(0, 0, 0d, 0)
            : new LoanStats(durations.Count, durations.Sum(), durations.Average(), durations.Max());
    }

    // 5) Топ-N через власний метод розширення TopBy: найновіші видання.
    public static IReadOnlyList<BookLine> NewestBooks(IEnumerable<BookDto> books, int n) =>
        books.TopBy(b => b.Year, n, b => b.Id)
             .Select(b => new BookLine(b.Id, b.Title, b.Year, b.Author))
             .ToList();

    // 6) Ще один звіт через TopBy: найдовші завершені видачі.
    public static IReadOnlyList<LoanLengthLine> LongestLoans(IEnumerable<Loan> loans, int n) =>
        loans.Where(l => l.IsClosed)
             .TopBy(Duration, n, l => l.Id)
             .Select(l => new LoanLengthLine(l.Id, l.ReaderId, Duration(l)))
             .ToList();

    // 7) Join двох колекцій у зведений DTO (додаткове завдання 1):
    //    видачі x примірники (за CopyId) x видання (за ISBN).
    public static IReadOnlyList<LoanDetailLine> LoanDetails(
        IEnumerable<Loan> loans, IEnumerable<BookCopy> copies, IEnumerable<BookDto> books) =>
        loans.Join(copies, l => l.CopyId, c => c.Id, (l, c) => new { Loan = l, Copy = c })
             .Join(books, x => x.Copy.Isbn, b => b.Isbn,
                   (x, b) => new LoanDetailLine(
                       x.Loan.Id, x.Loan.ReaderId, b.Title, x.Loan.IssuedOn, !x.Loan.IsClosed))
             .OrderBy(d => d.LoanId, StringComparer.Ordinal)
             .ToList();

    // 8) Той самий звіт 3, але в синтаксисі запитів (додаткове завдання 3).
    public static IReadOnlyList<BookGroup> BooksByCenturyQuerySyntax(IEnumerable<BookDto> books) =>
        (from b in books
         group b by (b.Year - 1) / 100 + 1 into g
         orderby g.Key
         select new BookGroup(
             $"{g.Key} ст.", g.Count(), g.Min(b => b.Year), g.Max(b => b.Year)))
        .ToList();

    // --- Перевірки для самоперевірки (щоб у Program.cs не було LINQ) ---

    // Порядок топ-N не залежить від порядку елементів у джерелі.
    public static bool TopReadersIsOrderIndependent(IReadOnlyList<Loan> loans, int n)
    {
        IReadOnlyList<ReaderLoanCount> forward = TopReaders(loans, n);
        IReadOnlyList<ReaderLoanCount> backward = TopReaders(loans.Reverse().ToList(), n);
        return forward.SequenceEqual(backward);
    }

    // Методний синтаксис і синтаксис запитів дають однаковий результат.
    public static bool CenturySyntaxesAgree(IEnumerable<BookDto> books) =>
        BooksByCentury(books).SequenceEqual(BooksByCenturyQuerySyntax(books));
}
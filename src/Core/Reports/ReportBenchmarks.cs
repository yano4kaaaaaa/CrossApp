using Core.Domain;

namespace Core.Reports;

// Додаткове завдання 4: той самий звіт ("скільки відкритих видач і топ-10 читачів за ними")
// у трьох варіантах, що відрізняються лише місцем ToList().
public static class ReportBenchmarks
{
    // A: без проміжного ToList. open — це ЗАПИТ, тому Count() і GroupBy
    //    перебирають джерело окремо: усього два проходи по всіх записах.
    public static BenchResult WithoutMaterialization(IEnumerable<Loan> loans)
    {
        var open = loans.Where(l => !l.IsClosed);          // запит, не результат

        int total = open.Count();                          // 1-й прохід по джерелу

        List<ReaderLoanCount> top = open
            .GroupBy(l => l.ReaderId)
            .Select(g => new ReaderLoanCount(g.Key, g.Count()))
            .OrderByDescending(r => r.Loans)
            .ThenBy(r => r.ReaderId, StringComparer.Ordinal)
            .Take(10)
            .ToList();                                     // 2-й прохід по джерелу

        return new BenchResult(total, top.Count);
    }

    // B: один ToList після Where. Джерело перебирається один раз,
    //    далі працюємо з готовим (меншим) списком відкритих видач.
    public static BenchResult MaterializeOnce(IEnumerable<Loan> loans)
    {
        List<Loan> open = loans.Where(l => !l.IsClosed).ToList();   // один прохід

        int total = open.Count;                                     // властивість, без проходу

        List<ReaderLoanCount> top = open
            .GroupBy(l => l.ReaderId)
            .Select(g => new ReaderLoanCount(g.Key, g.Count()))
            .OrderByDescending(r => r.Loans)
            .ThenBy(r => r.ReaderId, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        return new BenchResult(total, top.Count);
    }

    // C: ToList після кожного кроку. Результат той самий, але на кожному кроці
    //    створюється зайва копія колекції в пам'яті.
    public static BenchResult MaterializeEverywhere(IEnumerable<Loan> loans)
    {
        List<Loan> open = loans.Where(l => !l.IsClosed).ToList();
        int total = open.Count;

        var groups = open.GroupBy(l => l.ReaderId).ToList();
        var counted = groups.Select(g => new ReaderLoanCount(g.Key, g.Count())).ToList();
        var sorted = counted.OrderByDescending(r => r.Loans)
                            .ThenBy(r => r.ReaderId, StringComparer.Ordinal)
                            .ToList();
        var top = sorted.Take(10).ToList();

        return new BenchResult(total, top.Count);
    }
}
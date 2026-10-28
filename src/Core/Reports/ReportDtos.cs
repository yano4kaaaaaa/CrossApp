namespace Core.Reports;

// DTO звітів: незмінні record-и. Звіти повертають їх, а не доменні сутності Loan / BookCopy.

public sealed record ReaderLoanCount(string ReaderId, int Loans);

public sealed record OverdueLoanLine(
    string LoanId,
    string CopyId,
    string ReaderId,
    DateTime IssuedOn,
    DateTime DueOn,
    int DaysOverdue);

public sealed record BookGroup(string Century, int Titles, int OldestYear, int NewestYear);

public sealed record LoanStats(int Closed, int TotalDays, double AverageDays, int MaxDays);

public sealed record BookLine(string Id, string Title, int Year, string? Author);

public sealed record LoanLengthLine(string LoanId, string ReaderId, int Days);

public sealed record LoanDetailLine(
    string LoanId,
    string ReaderId,
    string Title,
    DateTime IssuedOn,
    bool IsOpen);

// Результат демо відкладеного виконання (крок 3 методички).
public sealed record DeferredDemoResult(int Before, int After, int SnapshotCount, int QueryAfterClear);

// Результат одного варіанта бенчмарка (додаткове завдання 4).
public sealed record BenchResult(int OpenLoans, int TopReaders);
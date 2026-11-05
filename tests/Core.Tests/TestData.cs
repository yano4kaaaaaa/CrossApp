using Core.Domain;
using Core.Dto;

namespace Core.Tests;

public static class TestData
{
    public static readonly DateTime Today = new(2026, 11, 4);

    public static List<BookCopy> Copies() =>
    [
        BookCopy.Create("C-1", "978-0-00-000001-1"),
        BookCopy.Create("C-2", "978-0-00-000002-2"),
        BookCopy.Create("C-3", "978-0-00-000003-3", isIssued: true),
    ];

    private static Loan L(string id, string reader, int daysAgo) =>
        Loan.FromDto(new LoanDto(id, "C-" + id, reader, Today.AddDays(-daysAgo), null));

    // R1 — 3 видачі, R2 — 2, R3 — 1 (жодних нічиїх: топ детермінований).
    public static List<Loan> Loans() =>
    [
        L("1", "R1", 5), L("2", "R1", 4), L("3", "R1", 3),
        L("4", "R2", 6), L("5", "R2", 2),
        L("6", "R3", 1),
    ];
}
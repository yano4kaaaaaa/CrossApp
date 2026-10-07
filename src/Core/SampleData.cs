using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<BookCopy> BookCopies() =>
    [
        BookCopy.Create("C-001", "978-0-13-468599-1"),
        BookCopy.Create("C-002", "978-966-03-5219-4", isIssued: true),
        BookCopy.Create("C-003", "978-617-7535-24-8"),
        BookCopy.Create("C-004", "978-966-14-9876-3"),
        BookCopy.Create("C-005", "978-0-596-00712-6", isIssued: true),
        BookCopy.Create("C-006", "978-966-97756-1-2"),
        BookCopy.Create("C-007", "978-617-664-123-4"),
        BookCopy.Create("C-008", "978-0-13-235088-4"),
        BookCopy.Create("C-009", "978-966-97025-3-1", isIssued: true),
        BookCopy.Create("C-010", "978-966-8659-38-6"),
        BookCopy.Create("C-011", "978-966-03-9999-1"),
        BookCopy.Create("C-012", "978-966-03-8888-2"),
        BookCopy.Create("C-013", "978-0-13-110362-7"),
        BookCopy.Create("C-014", "978-1-234567-89-0", isIssued: true),
        BookCopy.Create("C-015", "978-9-87654-32-1"),
        BookCopy.Create("C-016", "978-1-111111-1-1"),
        BookCopy.Create("C-017", "978-1-222222-2-2"),
        BookCopy.Create("C-018", "978-1-333333-3-3"),
    ];
}
using Core.Domain;
using Core.Dto;

namespace Core;

public static class SampleData
{
    // Без змін з лабораторної 5: 18 примірників, 4 з них видані (C-002, C-005, C-009, C-014).
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

    // Каталог видань (лабораторна 7): 18 назв, ISBN збігаються з ISBN примірників вище.
    // Роки охоплюють три століття, щоб GroupBy показав кілька груп.
    public static IReadOnlyList<BookDto> Books() =>
    [
        new BookDto("B-001", "978-0-13-468599-1", "Чиста архітектура", 2017, "Роберт Мартін"),
        new BookDto("B-002", "978-966-03-5219-4", "Кобзар", 1840, "Тарас Шевченко"),
        new BookDto("B-003", "978-617-7535-24-8", "Лісова пісня", 1911, "Леся Українка"),
        new BookDto("B-004", "978-966-14-9876-3", "Тіні забутих предків", 1912, "Михайло Коцюбинський"),
        new BookDto("B-005", "978-0-596-00712-6", "Head First Design Patterns", 2004, "Eric Freeman"),
        new BookDto("B-006", "978-966-97756-1-2", "Захар Беркут", 1883, "Іван Франко"),
        new BookDto("B-007", "978-617-664-123-4", "Місто", 1928, "Валер'ян Підмогильний"),
        new BookDto("B-008", "978-0-13-235088-4", "Clean Code", 2008, "Robert C. Martin"),
        new BookDto("B-009", "978-966-97025-3-1", "Тигролови", 1944, "Іван Багряний"),
        new BookDto("B-010", "978-966-8659-38-6", "Хіба ревуть воли, як ясла повні", 1875, "Панас Мирний"),
        new BookDto("B-011", "978-966-03-9999-1", "Тигролови (друге видання)", 1944, "Іван Багряний"),
        new BookDto("B-012", "978-966-03-8888-2", "Кайдашева сім'я", 1879, "Іван Нечуй-Левицький"),
        new BookDto("B-013", "978-0-13-110362-7", "The C Programming Language", 1978, "Brian Kernighan"),
        new BookDto("B-014", "978-1-234567-89-0", "Основи алгоритмів", 2010, "Роберт Седжвік"),
        new BookDto("B-015", "978-9-87654-32-1", "Зачарована Десна", 1957, "Олександр Довженко"),
        new BookDto("B-016", "978-1-111111-1-1", "Вступ до програмування", 2019),   // автор невідомий (null)
        new BookDto("B-017", "978-1-222222-2-2", "Основи C#", 2021, "Олена Коваль"),
        new BookDto("B-018", "978-1-333333-3-3", "Патерни проєктування", 2015, "Іван Мельник"),
    ];

    // Історія видач (лабораторна 7). Дати задаються відносно today, тому кількість
    // прострочених видач не залежить від дня запуску.
    // 4 відкриті видачі відповідають 4 виданим примірникам з BookCopies().
    // Видачі створюються через Loan.FromDto: сутність відновлюється з усіма перевірками
    // інваріантів, але без зміни стану примірників.
    public static IReadOnlyList<Loan> Loans(DateTime today)
    {
        static Loan L(string id, string copyId, string readerId, DateTime issued, DateTime? returned) =>
            Loan.FromDto(new LoanDto(id, copyId, readerId, issued, returned));

        return
        [
            // --- відкриті видачі ---
            L("L-001", "C-002", "R-100", today.AddDays(-30), null),
            L("L-002", "C-005", "R-101", today.AddDays(-20), null),
            L("L-003", "C-009", "R-100", today.AddDays(-5),  null),
            L("L-004", "C-014", "R-102", today.AddDays(-15), null),

            // --- завершені видачі ---
            L("L-101", "C-001", "R-100", today.AddDays(-120), today.AddDays(-110)),
            L("L-102", "C-003", "R-101", today.AddDays(-100), today.AddDays(-90)),
            L("L-103", "C-004", "R-103", today.AddDays(-90),  today.AddDays(-70)),
            L("L-104", "C-006", "R-100", today.AddDays(-85),  today.AddDays(-80)),
            L("L-105", "C-007", "R-104", today.AddDays(-80),  today.AddDays(-60)),
            L("L-106", "C-008", "R-101", today.AddDays(-70),  today.AddDays(-63)),
            L("L-107", "C-010", "R-102", today.AddDays(-65),  today.AddDays(-50)),
            L("L-108", "C-011", "R-100", today.AddDays(-60),  today.AddDays(-58)),
            L("L-109", "C-012", "R-103", today.AddDays(-55),  today.AddDays(-40)),
            L("L-110", "C-013", "R-105", today.AddDays(-50),  today.AddDays(-30)),
            L("L-111", "C-015", "R-101", today.AddDays(-45),  today.AddDays(-41)),
            L("L-112", "C-016", "R-104", today.AddDays(-40),  today.AddDays(-33)),
            L("L-113", "C-017", "R-100", today.AddDays(-35),  today.AddDays(-28)),
            L("L-114", "C-018", "R-102", today.AddDays(-30),  today.AddDays(-26)),
        ];
    }
}
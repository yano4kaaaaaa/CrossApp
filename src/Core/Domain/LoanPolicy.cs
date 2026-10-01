using System.Linq;

namespace Core.Domain;

// Додаткове завдання 2 (тиждень 4): інваріант, що охоплює ДВІ сутності (Loan + Reader),
// а точніше — КОЛЕКЦІЮ видач одного читача. Жодна окрема сутність (ні BookCopy, ні один
// Loan) фізично не має доступу до "всіх видач цього читача" — кожен Loan знає лише про
// самого себе. Тому таке правило не можна коректно реалізувати всередині Loan.Open():
// довелось би передавати туди повний список усіх видач системи, що перетворило б просту
// сутність на сервіс із прихованими залежностями.
//
// Правильне місце для такої перевірки — сервіс (тиждень 5, CatalogService/LoanService),
// який має доступ до сховища й може опитати ВСІ видачі читача перед тим, як дозволити
// Loan.Open(). Тут це представлено як окремий статичний клас-політика, що моделює,
// яким саме буде виклик цієї перевірки з майбутнього сервісу.
public static class LoanPolicy
{
    public const int MaxOpenLoansPerReader = 5;

    public static void EnsureReaderCanBorrow(string readerId, IReadOnlyList<Loan> existingLoans)
    {
        int openCount = existingLoans.Count(l => l.ReaderId == readerId && !l.IsClosed);

        if (openCount >= MaxOpenLoansPerReader)
            throw new InvalidOperationException(
                $"Читач {readerId} вже має {openCount} відкритих видач (ліміт {MaxOpenLoansPerReader}) — нова видача неможлива");
    }
}
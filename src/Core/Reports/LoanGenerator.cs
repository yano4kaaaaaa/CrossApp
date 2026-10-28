using Core.Domain;
using Core.Dto;

namespace Core.Reports;

public static class LoanGenerator
{
    // Детермінована генерація (фіксований seed): кожен запуск дає ті самі дані.
    public static List<Loan> Generate(int count, DateTime today, int seed = 42)
    {
        var rnd = new Random(seed);
        var list = new List<Loan>(count);

        for (int i = 0; i < count; i++)
        {
            DateTime issued = today.AddDays(-rnd.Next(1, 365));

            // приблизно 80% видач уже повернено
            DateTime? returned = rnd.Next(100) < 80
                ? issued.AddDays(rnd.Next(1, 40))
                : null;

            var dto = new LoanDto(
                $"L-{i:D6}",
                $"C-{rnd.Next(1, 5000):D5}",
                $"R-{rnd.Next(1, 2000):D4}",
                issued,
                returned);

            list.Add(Loan.FromDto(dto));
        }

        return list;
    }
}
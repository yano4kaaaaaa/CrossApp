namespace Core.Reports;

public static class EnumerableExtensions
{
    // Додаткове завдання 2: власний метод розширення для топ-N.
    // Основний критерій — числовий ключ за спаданням; необов'язковий другий ключ
    // (tieBreaker) робить порядок детермінованим, коли значення збігаються.
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source,
        Func<T, int> key,
        int n,
        Func<T, string>? tieBreaker = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);

        IOrderedEnumerable<T> ordered = source.OrderByDescending(key);

        IOrderedEnumerable<T> result = tieBreaker is null
            ? ordered
            : ordered.ThenBy(tieBreaker, StringComparer.Ordinal);

        return result.Take(n);
    }
}
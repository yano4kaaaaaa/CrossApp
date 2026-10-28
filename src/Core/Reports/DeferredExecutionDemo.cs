using Core.Domain;

namespace Core.Reports;

public static class DeferredExecutionDemo
{
    // Чому два виклики Count() дають різні числа:
    // 1) query = source.Where(...) лише ОПИСУЄ запит, нічого не рахує.
    // 2) Count() запускає перебирання джерела на момент виклику.
    // 3) Між двома Count() ми додали елемент у source, тому другий прохід бачить нові дані.
    // 4) ToList() фіксує результат ("знімок"): він не змінюється, коли джерело змінюється далі.
    public static DeferredDemoResult Run(IEnumerable<BookCopy> copies)
    {
        List<BookCopy> source = copies.ToList();   // локальний список, джерело для демо

        var query = source.Where(c => c.IsIssued); // запит побудовано, але НЕ виконано

        int before = query.Count();                // 1-й прохід: рахує поточні видані

        source.Add(BookCopy.Create("tmp00001", "978-0-000000-0-0", isIssued: true));

        int after = query.Count();                 // 2-й прохід: бачить доданий елемент

        List<BookCopy> snapshot = query.ToList();  // результат зафіксовано

        source.Clear();                            // джерело очищено

        int afterClear = query.Count();            // запит виконується знову: тепер 0

        return new DeferredDemoResult(before, after, snapshot.Count, afterClear);
    }
}
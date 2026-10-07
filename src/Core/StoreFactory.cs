using Core.Abstractions;
using Core.Storage;

namespace Core;

// Додаткове завдання 3: та сама логіка вибору реалізації, що раніше стояла прямо
// в Program.cs (тернарний вибір), тепер винесена в окремий метод. Program.cs стає ще
// коротшим і не знає деталей побудови сховища — лише викликає фабрику.
public static class StoreFactory
{
    public static IBookStore Create(string[] args, string dataPath)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        IBookStore store = useFile
            ? new FileBookStore(dataPath)
            : new InMemoryBookStore(SampleData.BookCopies());

        return useCache ? new CachingBookStore(store) : store;
    }
}
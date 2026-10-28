# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: **Бібліотека**.
Сутності: `Book` (видання), `BookCopy` (примірник), `Reader` (читач), `Loan` (видача).
Призначення: облік видач примірників книг читачам і повернень.

## Запуск

​```bash
dotnet build
dotnet run --project src/Cli
​```

## Середовище

.NET SDK 10.0.401, Windows 11 x64

## Публікація self-contained (лабораторна 1)

​```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
# або linux-x64 / osx-arm64
​```

Опубліковано під двома RID:

- win-x64: 76,84 MB
- linux-x64: 78,80 MB

Розміри порівнянні, бо в обох випадках разом з застосунком пакується весь .NET runtime.

## Лабораторна 2: Core + Cli

### Структура solution

- `src/Core` — бібліотека класів (Core.csproj, TargetFrameworks: net8.0;net10.0), збирає інформацію про середовище
- `src/Cli` — консольний застосунок (Cli.csproj), має ProjectReference на Core і лише форматує вивід

### Команди

​```bash
dotnet build
dotnet run --project src/Cli
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
​```

### Порівняння режимів публікації

| RID       | Режим                                 | Розмір publish | Файлів | Потрібен runtime |
|-----------|----------------------------------------|-----------------|--------|-------------------|
| win-x64   | self-contained                         | 76,86 MB        | ~230   | ні                |
| win-x64   | framework-dependent                    | 0,20 MB         | кілька | так (.NET 10)     |
| win-x64   | self-contained + PublishSingleFile     | 70,15 MB        | 3      | ні                |
| win-x64   | self-contained + PublishTrimmed        | 19,54 MB        | ~15    | ні (падає на --json) |
| linux-x64 | self-contained                         | 78,80 MB        | ~230   | ні                |

**PublishTrimmed:** dotnet publish видав 2 попередження IL2026 про `JsonSerializer.Serialize` (використовує рефлексію). Перевірено на практиці: звичайний запуск працює нормально, але `Cli.exe --json` падає з `InvalidOperationException: Reflection-based serialization has been disabled` — trimming видалив метадані типів, потрібні для JSON-серіалізації.

**Multi-targeting:** `Core.csproj` зібрано під `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>` без помилок. У виводі `Cli` (зібраний під net10.0) поле "Примітка збірки" показує `збірка під net10.0` завдяки директиві `#if NET10_0_OR_GREATER` в `EnvironmentInfo.cs`.

### Домовленість про каталоги Core (на весь семестр)

- `Core/Dto/` — record-типи формату даних (тиждень 3)
- `Core/Domain/` — сутності з поведінкою (тиждень 4)
- `Core/Storage/` — реалізації сховищ (тиждень 5)

## Лабораторна 7: звіти через LINQ

Усі запити лежать у `src/Core/Reports/ReportService.cs`. Вони приймають `IEnumerable<T>`
(а не сховище), повертають `IReadOnlyList<DTO>` і нічого не знають про `Console` та файли.
У `Program.cs` немає жодного `Where` / `GroupBy`: лише виклики `ReportService` і виведення.

### Запити

| Метод | Питання, на яке відповідає | Оператори |
|---|---|---|
| `OverdueLoans` | Які видачі прострочені? | `Where`, `OrderByDescending`, `ThenBy`, `Select`, `ToList` |
| `TopReaders` | Хто бере книги найчастіше? (топ-5) | `GroupBy`, `Select`, `OrderByDescending`, `ThenBy`, `Take` |
| `BooksByCentury` | Як розподілені видання за століттями? | `GroupBy`, `OrderBy`, `Select` (`g.Key`, `g.Count()`, `g.Min`, `g.Max`) |
| `LoanStatistics` | Яка тривалість завершених видач? | `Where`, `Select`, `Sum`, `Average`, `Max`, перевірка порожньої послідовності |
| `NewestBooks` | Які видання найновіші? | власний `TopBy`, `Select` |
| `LongestLoans` | Які видачі найдовші? | `Where`, власний `TopBy`, `Select` |
| `LoanDetails` | Назви книг у видачах | `Join` x2, `OrderBy` |

DTO звітів: `ReaderLoanCount`, `OverdueLoanLine`, `BookGroup`, `LoanStats`, `BookLine`,
`LoanLengthLine`, `LoanDetailLine`. Звіт не повертає `Loan` чи `BookCopy`, тому контракт звіту
не залежить від внутрішньої структури доменної моделі.

### Підкоманди Cli

```bash
dotnet run --project src/Cli -- report-top        # топ-5 читачів
dotnet run --project src/Cli -- report-overdue    # прострочені видачі
dotnet run --project src/Cli -- report-books      # видання за століттями (GroupBy)
dotnet run --project src/Cli -- report-stats      # агрегати (Sum/Average/Max)
dotnet run --project src/Cli -- report-newest     # топ-5 найновіших (TopBy)
dotnet run --project src/Cli -- report-longest    # топ-5 найдовших видач (TopBy)
dotnet run --project src/Cli -- report-details    # Join
dotnet run --project src/Cli -- report-query      # той самий GroupBy у синтаксисі запитів
dotnet run --project src/Cli -- report-stable     # порядок топ-N не залежить від джерела
dotnet run --project src/Cli -- report-empty      # звіти на порожньому джерелі
dotnet run --project src/Cli -- report-defer      # демо відкладеного виконання
dotnet run --project src/Cli -- report-bench      # Stopwatch на 100 000 записів
dotnet run --project src/Cli -- report-all        # усе, крім bench
```

Прапорці (`--file`, `--cache`) не вважаються назвою звіту.

### Відкладене виконання

Рядок `source.Where(c => c.IsIssued)` лише описує запит і нічого не рахує. Обчислення
починається в момент виклику `Count()` чи `ToList()`, і кожен такий виклик перебирає джерело
заново. Тому перший `Count()` дав 4 (до змін), а другий 5: між ними в `source` додали ще один
виданий примірник, і другий прохід його побачив. Виклик `ToList()` зробив знімок (5 елементів),
який уже не змінюється, а після `source.Clear()` сам запит дає 0, бо виконується наново.
Демо: `DeferredExecutionDemo.Run`, команда `report-defer`.

### Детермінований топ-N

`OrderByDescending` має другий ключ `ThenBy(ReaderId, StringComparer.Ordinal)`. Читачі `R-103` і
`R-104` мають по 2 видачі, тож без другого ключа їхній порядок залежав би від порядку в джерелі.
Команда `report-stable` перевіряє це: результат на прямому й зворотному порядку джерела однаковий.

### Додаткові завдання

1. **Join:** `LoanDetails` з'єднує видачі, примірники (за `CopyId`) і видання (за ISBN) у `LoanDetailLine`.
2. **Власний метод розширення:** `TopBy<T>(this IEnumerable<T>, Func<T,int>, int, Func<T,string>?)`
   використано у двох звітах: `NewestBooks` і `LongestLoans`.
3. **Синтаксис запитів:** `BooksByCenturyQuerySyntax`. Порівняння: синтаксис запитів
   (`from ... group ... into ... select`) читається як речення й зручний для групування з проміжною
   змінною `g`, але не має еквівалента для `Take`, `Distinct` чи власних методів, тому ланцюжок
   доводиться загортати в дужки з `.ToList()`. Методний синтаксис коротший для простих
   фільтрів і єдиний підтримує все (`TopBy`). Обидва варіанти дають однаковий результат
   (`CenturySyntaxesAgree` повертає `True`).
4. **Stopwatch** (`report-bench`, 100 000 згенерованих видач, середнє з 30 запусків):

   | Варіант | Мс |
   |---|---|
   | A: без проміжного `ToList` (два проходи по джерелу) | _впишіть свій_ |
   | B: один `ToList` після `Where` | _впишіть свій_ |
   | C: `ToList` після кожного кроку | _впишіть свій_ |

   Висновок: `ToList` потрібен там, де запит використовується двічі (A проти B), але ставити його
   після кожного кроку немає сенсу (C): кожен зайвий `ToList` створює копію колекції.

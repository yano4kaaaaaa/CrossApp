# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: **Бібліотека**.
Сутності: `Book` (видання), `BookCopy` (примірник), `Reader` (читач), `Loan` (видача).
Призначення: облік видач примірників книг читачам і повернень.

## Запуск

```bash
dotnet build
dotnet run --project src/Cli
```

## Середовище

.NET SDK 10.0.401, Windows 11 x64

## Публікація self-contained (лабораторна 1)

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
# або linux-x64 / osx-arm64
```

Опубліковано під двома RID:

- win-x64: 76,84 MB
- linux-x64: 78,80 MB

Розміри порівнянні, бо в обох випадках разом з застосунком пакується весь .NET runtime.

## Лабораторна 2: Core + Cli

### Структура solution

- `src/Core` — бібліотека класів (Core.csproj, TargetFrameworks: net8.0;net10.0), збирає інформацію про середовище
- `src/Cli` — консольний застосунок (Cli.csproj), має ProjectReference на Core і лише форматує вивід

### Команди

```bash
dotnet build
dotnet run --project src/Cli
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
```

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
## Лабораторна 4: доменна модель (BookCopy + Loan)

### Сутності

- `src/Core/Domain/BookCopy.cs` — примірник книги: ISBN, стан видачі (`IsIssued`).
- `src/Core/Domain/Loan.cs` — видача: зв'язує примірник і читача, дати видачі/повернення, явний стан `LoanStatus`.

DTO тижня 3 (`BookDto`, `ReaderDto`) лишаються форматом даних для імпорту; нові DTO
(`BookCopyDto`, `LoanDto`) — формат зберігання стану сутностей (`ToDto`/`FromDto`).

### Інваріанти

| Правило | Тип винятку | Де перевіряється |
|---|---|---|
| ISBN примірника не порожній | `ArgumentException` | `BookCopy.Create` |
| Ідентифікатор примірника не порожній | `ArgumentException` | `BookCopy.Create` |
| Не можна видати вже виданий примірник | `InvalidOperationException` | `BookCopy.Issue` |
| Не можна повернути не виданий примірник | `InvalidOperationException` | `BookCopy.Return` |
| Ідентифікатор читача не порожній | `ArgumentException` | `Loan.Open` |
| Дата повернення не раніше дати видачі | `ArgumentOutOfRangeException` | `Loan.Close` / `Loan.FromDto` |
| Перехід стану Closed → Closed заборонений | `InvalidOperationException` | `Loan.Close` (через `EnsureTransition`) |

### Інкапсуляція

- Конструктори `BookCopy` і `Loan` — приватні; єдиний спосіб створення — фабричні методи
  `Create` / `Open` / `FromDto`, які перевіряють усі інваріанти ДО створення об'єкта.
- `IsIssued`, `ReturnedOn` — властивості з `private set`: читаються звідусіль, змінюються лише
  методами класу (`Issue`, `Return`, `Close`).
- `Core/Domain` не залежить від `Console` чи `File` — лише бізнес-правила.

### Запуск

```bash
dotnet run --project src/Cli
```

Виводить: успішний сценарій, п'ять порушень інваріантів, і три блоки додаткових завдань.

### Додаткові завдання

1. **Зв'язок із тижнем 3** (`BookCopyCsvImporter` + `BookCopyImportAdapter`): `data/copies.csv`
   імпортується у `BookCopyDto`, потім перетворюється на сутності `BookCopy`. Помилки
   збираються на ДВОХ рівнях: структурні (неправильний формат `bool`) — ще на етапі CSV,
   і доменні (порожній ISBN) — уже при спробі створити сутність через `FromDto`.
2. **Інваріант на дві сутності** (`LoanPolicy.EnsureReaderCanBorrow`): читач не може мати
   більше 5 відкритих видач одночасно. Правило не можна реалізувати всередині `Loan` чи
   `BookCopy` окремо — жодна сутність не має доступу до повного списку видач читача;
   таке місце — сервіс (тиждень 5), тут представлено статичним класом-політикою.
3. **Явний стан `LoanStatus`** (`enum { Open, Closed }`): замість обчислюваного `bool IsClosed`
   додано явний стан і перевірка допустимих переходів через `switch` у `EnsureTransition`.
   ## Лабораторна 5: сервісний шар (IBookStore + LendingService)

### Інтерфейс

`src/Core/Abstractions/IBookStore.cs` — контракт сховища, 5 методів:

```csharp
public interface IBookStore
{
    IReadOnlyList<BookCopy> List();
    BookCopy? GetById(string id);
    void Add(BookCopy item);
    void Update(BookCopy item);
    bool Remove(string id);
}
```

### Реалізації

- **`InMemoryBookStore`** (`Core/Storage`) — зберігає записи у `Dictionary<string, BookCopy>`
  у пам'яті; дані зникають після завершення програми. Наповнюється через `SampleData`
  (18 примірників).
- **`FileBookStore`** (`Core/Storage`) — кеш у пам'яті + синхронізація з
  `data/library.json` у форматі `BookCopyDto` (System.Text.Json). Читає файл один раз
  при першому зверненні (`EnsureLoaded`), записує після кожної зміни (`Flush`).
- **`CachingBookStore`** (додаткове завдання 1) — декоратор: приймає будь-який інший
  `IBookStore` у конструкторі, кешує результат `List()`, скидає кеш після `Add`/`Update`/`Remove`.

### Сервіс

`src/Core/Services/LendingService.cs` залежить лише від `IBookStore` (інтерфейсу, не класу),
отримує його через конструктор (ручний DI). Операції: `AddBook`, `IssueCopy`, `ReturnCopy`,
`All`, `Find(id)`, і `Find(predicate)` (додаткове завдання 2).

### Composition root

`src/Cli/Program.cs` — єдине місце, де створюються конкретні класи сховищ (через
`StoreFactory`, додаткове завдання 3). Ніде більше в `Core` немає `new FileBookStore(...)`
чи подібного.

```csharp
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");
IBookStore store = StoreFactory.Create(args, dataPath);
var service = new LendingService(store);
```

### Режими запуску

```bash
dotnet run --project src/Cli            # InMemoryBookStore, 18 прикладів із SampleData
dotnet run --project src/Cli -- --file  # FileBookStore, дані зберігаються в data/library.json
dotnet run --project src/Cli -- --cache # CachingBookStore поверх InMemoryBookStore
```

Два запуски з `--file` накопичують дані (не обнуляють файл).

### Додаткові завдання

1. **`CachingBookStore`** — декоратор над `IBookStore`, той самий контракт, нова поведінка
   (кешування `List()`).
2. **`LendingService.Find(Func<BookCopy, bool> predicate)`** — пошук за довільною умовою,
   наприклад `service.Find(c => c.IsIssued)`.
3. **`StoreFactory.Create(args, dataPath)`** — вибір реалізації винесено з `Program.cs`
   в окремий метод; `Program.cs` більше не знає деталей побудови сховища.
   ## Лабораторна 6: доменні події, делегати, лямбди

### Які зміни вважаються подіями

| Подія | Коли виникає | Дані (`LendingChangedEventArgs`) |
|---|---|---|
| `BookAdded` | після успішного `AddBook` | `CopyId` |
| `CopyIssued` | після успішного `IssueCopy` | `CopyId`, `ReaderId`, `DueDate` |
| `CopyReturned` | після успішного `ReturnCopy` | `CopyId` |

Подія піднімається ЛИШЕ після успішної зміни стану. Якщо інваріант кинув виняток
(повторна видача, порожній читач, неіснуючий id) — повідомлення немає.

### Код

- `src/Core/Events/LendingChangeKind.cs` — enum типу події.
- `src/Core/Events/LendingChangedEventArgs.cs` — дані події (успадковує `EventArgs`).
- `src/Core/Services/LendingService.cs` — `event EventHandler<LendingChangedEventArgs>? Changed`,
  приватний `OnChanged` з `Changed?.Invoke(this, args)`, метод `FindAll(Func<BookCopy,bool>)`.
- `Core` не містить `Console` і не пише в лог-файл.

### Підписники в Cli

1. **Локальний метод** `OnLendingChanged` — друкує рядок у консоль і дописує в `logs/app.log`
   (шлях будується через `Path.Combine`, каталог створюється кодом).
2. **Лямбда-лічильник** `(_, _) => changes++` — замикання над локальною змінною `changes`.

Після `service.Changed -= OnLendingChanged` обробник мовчить (у логу немає рядка), а лічильник
і далі спрацьовує.

### Запуск

```bash
dotnet run --project src/Cli
```

Лог: `src/Cli/bin/Debug/net10.0/logs/app.log`.

### Подія чи Func

- **Подія** — «щось сталося», відповіді не чекаємо (`Changed`).
- **Func** — потрібне значення у відповідь: `service.FindAll(c => !c.IsIssued)`.

### Додаткові завдання

1. **`INotificationSink`** (`ConsoleSink`, `FileSink` у Cli) передаються сервісу через конструктор.
   Порівняння з подією:
   - подія: сервіс не знає підписників, їх може бути 0..N, підписка/відписка під час роботи;
   - sink: список одержувачів фіксований при створенні сервісу, зате залежність явна в
     конструкторі й видна в сигнатурі; відписки немає.
2. **`Action<string>`** — `BookCsvImporter.Load(path, onSkipped)` викликає callback для кожного
   пропущеного рядка (результат `ImportResult` лишається без змін).
3. **Чому async-обробник події — це `async void`.** Тип `EventHandler` повертає `void`, тому
   обробник з `async` теж має бути `async void`. Така функція не повертає `Task`, і викликач не може
   дочекатися її завершення (`await`) та перехопити виняток: виняток із `async void` не потрапляє в
   `try/catch` навколо `Changed?.Invoke(...)`, а піднімається в контексті синхронізації й може
   завершити весь процес. Тому ризиковану роботу всередині такого обробника обгортають у власний
   `try/catch` (детально — тиждень 8).

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

   # Лабораторна 8 — async/await, скасування, доменні винятки, тести

## Запуск
    dotnet run --project src/Cli -- import                    # імпорт data/import.json
    dotnet run --project src/Cli -- import-stream             # потоковий (IAsyncEnumerable), лічильник
    dotnet run --project src/Cli -- import --path=data/import-bad.json   # "Дані відхилено: Запис 2: ..."

## Перевірка скасування
    dotnet run --project src/Cli -- import-gen                # 200 000 записів
    dotnet run --project src/Cli -- import-stream             # натисніть Ctrl+C -> "Імпорт скасовано."
    dotnet run --project src/Cli -- import --timeout=0.05     # таймаут -> "Таймаут: ..."

## Тести
    dotnet test
    dotnet test --collect:"XPlat Code Coverage"

Проєкт tests/Core.Tests посилається лише на Core.
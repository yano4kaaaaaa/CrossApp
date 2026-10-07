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
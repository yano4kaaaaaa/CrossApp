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

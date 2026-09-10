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

## Публікація self-contained (опційно)

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
# або linux-x64 / osx-arm64
```
## Лабораторна 2: Core + Cli

### Структура solution
- src/Core — бібліотека класів (Core.csproj), збирає інформацію про середовище
- src/Cli — консольний застосунок (Cli.csproj), має ProjectReference на Core і лише форматує вивід

### Команди
dotnet build
dotnet run --project src/Cli
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false

### Порівняння режимів публікації

| RID       | Режим               | Розмір publish | Потрібен runtime |
|-----------|---------------------|-----------------|-------------------|
| win-x64   | self-contained       | 76,86 MB        | ні                |
| win-x64   | framework-dependent   | 0,20 MB         | так (.NET 10)     |
| linux-x64 | self-contained       | 78,80 MB        | ні                |
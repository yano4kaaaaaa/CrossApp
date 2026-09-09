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
## Self-contained публікація

Опубліковано під двома RID (`dotnet publish -c Release -r <RID> --self-contained true`):

- win-x64: 76,84 MB
- linux-x64: 78,80 MB

Розміри порівнянні, бо в обох випадках разом з застосунком пакується весь .NET runtime.
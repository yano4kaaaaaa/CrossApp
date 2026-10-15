using Core.Events;

namespace Core.Abstractions;

// Додаткове завдання 1: альтернатива події. Сервіс отримує одержувачів сповіщень
// через конструктор (ручний DI тижня 5) і сам викликає їх по черзі.
public interface INotificationSink
{
    void Notify(LendingChangedEventArgs e);
}
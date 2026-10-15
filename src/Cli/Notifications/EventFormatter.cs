using Core.Events;

namespace Cli.Notifications;

public static class EventFormatter
{
    public static string Format(LendingChangedEventArgs e)
    {
        string details = e.Kind switch
        {
            LendingChangeKind.BookAdded => "додано примірник",
            LendingChangeKind.CopyIssued => $"видано читачу {e.ReaderId} до {e.DueDate:dd.MM.yyyy}",
            LendingChangeKind.CopyReturned => "повернено до бібліотеки",
            _ => "невідома зміна"
        };

        return $"{e.At:HH:mm:ss} {e.Kind,-12} {e.CopyId,-9} {details}";
    }
}
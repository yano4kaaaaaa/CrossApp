using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(LendingChangedEventArgs e) =>
        Console.WriteLine($"  [sink:консоль] {EventFormatter.Format(e)}");
}
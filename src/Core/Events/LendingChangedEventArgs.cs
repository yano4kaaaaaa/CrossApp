namespace Core.Events;

public sealed class LendingChangedEventArgs(
    string copyId,
    LendingChangeKind kind,
    string? readerId = null,
    DateOnly? dueDate = null) : EventArgs
{
    public string CopyId { get; } = copyId;
    public LendingChangeKind Kind { get; } = kind;
    public string? ReaderId { get; } = readerId;   // лише для CopyIssued
    public DateOnly? DueDate { get; } = dueDate;   // лише для CopyIssued
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
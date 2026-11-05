using Core;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class LendingServiceTests
{
    [Fact] // інваріант: повернення невиданого примірника
    public void ReturnCopy_NotIssued_ThrowsDomainException_AndKeepsState()
    {
        var store = new InMemoryBookStore(TestData.Copies());
        var service = new LendingService(store);

        Assert.Throws<DomainException>(() => service.ReturnCopy("C-1"));
        Assert.False(store.GetById("C-1")!.IsIssued);
    }

    [Fact] // повторна видача — доменна помилка, події немає
    public void IssueCopy_AlreadyIssued_Throws_AndRaisesNoEvent()
    {
        var service = new LendingService(new InMemoryBookStore(TestData.Copies()));
        var events = new List<LendingChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<DomainException>(() => service.IssueCopy("C-3", "R1"));
        Assert.Empty(events);
    }

    [Fact] // некоректний аргумент — ArgumentException, а не DomainException
    public void IssueCopy_EmptyReader_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() =>
            new LendingService(new InMemoryBookStore(TestData.Copies())).IssueCopy("C-1", " "));

    [Fact] // подія як спосіб перевірки
    public void AddBook_RaisesChangedExactlyOnce()
    {
        var service = new LendingService(new InMemoryBookStore());
        var events = new List<LendingChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.AddBook("978-0-00-000009-9");

        Assert.Single(events);
        Assert.Equal(LendingChangeKind.BookAdded, events[0].Kind);
    }

    [Fact] // перехід стану видачі
    public void Loan_Close_Twice_ThrowsDomainException()
    {
        var copy = BookCopy.Create("C-9", "978-0-00-000009-9");
        var loan = Loan.Open(copy, "R1", TestData.Today);
        loan.Close(TestData.Today.AddDays(1));

        Assert.Throws<DomainException>(() => loan.Close(TestData.Today.AddDays(2)));
    }
}
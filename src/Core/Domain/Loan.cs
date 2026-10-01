using Core.Dto;

namespace Core.Domain;

// Додаткове завдання 3 (тиждень 4): явний стан-перелічування замість обчислюваного bool,
// і перевірка допустимих переходів між станами через switch expression.
public enum LoanStatus
{
    Open,
    Closed
}

public sealed class Loan
{
    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }
    public DateTime? ReturnedOn { get; private set; }

    public bool IsClosed => ReturnedOn.HasValue;

    public LoanStatus Status => IsClosed ? LoanStatus.Closed : LoanStatus.Open;

    private Loan(string id, string copyId, string readerId, DateTime issuedOn, DateTime? returnedOn)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
    }

    // Відкриває нову видачу: переводить примірник у стан "видано" і створює запис видачі.
    public static Loan Open(BookCopy copy, string readerId, DateTime issuedOn)
    {
        if (copy is null)
            throw new ArgumentNullException(nameof(copy));

        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));

        copy.Issue(); // кине InvalidOperationException, якщо примірник вже видано

        string id = $"L-{copy.Id}-{issuedOn:yyyyMMddHHmmss}";
        return new Loan(id, copy.Id, readerId.Trim(), issuedOn, returnedOn: null);
    }

    // Закриває видачу (без зміни примірника — повернення примірника окремим викликом copy.Return()).
    public void Close(DateTime returnedOn)
    {
        EnsureTransition(Status, LoanStatus.Closed);

        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(
                nameof(returnedOn), returnedOn,
                "Дата повернення не може бути раніше дати видачі");

        ReturnedOn = returnedOn;
    }

    // Перевіряє, чи дозволений перехід між станами, через switch expression.
    private static void EnsureTransition(LoanStatus from, LoanStatus to)
    {
        bool allowed = (from, to) switch
        {
            (LoanStatus.Open, LoanStatus.Closed) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"Перехід зі стану {from} у {to} неможливий");
    }

    // Відновлення з формату зберігання (тиждень 3/5): проходить ті самі перевірки, що й Open/Close.
    public static Loan FromDto(LoanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            throw new ArgumentException("Ідентифікатор видачі обов'язковий", nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.CopyId))
            throw new ArgumentException("Ідентифікатор примірника обов'язковий", nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.ReaderId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(dto));

        if (dto.ReturnedOn.HasValue && dto.ReturnedOn.Value < dto.IssuedOn)
            throw new ArgumentOutOfRangeException(
                nameof(dto), dto.ReturnedOn,
                "Дата повернення не може бути раніше дати видачі");

        return new Loan(dto.Id, dto.CopyId, dto.ReaderId, dto.IssuedOn, dto.ReturnedOn);
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn);

    public override string ToString() =>
        $"{Id}: примірник {CopyId}, читач {ReaderId}, видано {IssuedOn:d}" +
        (IsClosed ? $", повернено {ReturnedOn:d}" : ", не повернено");
}
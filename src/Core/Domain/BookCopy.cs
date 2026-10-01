using Core.Dto;

namespace Core.Domain;

public sealed class BookCopy
{
    public string Id { get; }
    public string Isbn { get; }
    public bool IsIssued { get; private set; }

    private BookCopy(string id, string isbn, bool isIssued)
    {
        Id = id;
        Isbn = isbn;
        IsIssued = isIssued;
    }

    // Єдиний спосіб створити примірник: усі перевірки тут.
    public static BookCopy Create(string id, string isbn, bool isIssued = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор примірника обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        return new BookCopy(id.Trim(), isbn.Trim(), isIssued);
    }

    public void Issue()
    {
        if (IsIssued)
            throw new InvalidOperationException(
                $"Примірник {Id} вже виданий, повторна видача неможлива");

        IsIssued = true;
    }

    public void Return()
    {
        if (!IsIssued)
            throw new InvalidOperationException(
                $"Примірник {Id} не видано, повернення неможливе");

        IsIssued = false;
    }

    // Мапінг у формат тижня 3 і назад — знадобиться сховищу тижня 5.
    public BookCopyDto ToDto() => new(Id, Isbn, IsIssued);

    public static BookCopy FromDto(BookCopyDto dto) => Create(dto.Id, dto.Isbn, dto.IsIssued);

    public override string ToString() =>
        $"{Id} [{Isbn}] — {(IsIssued ? "видано" : "у наявності")}";
}
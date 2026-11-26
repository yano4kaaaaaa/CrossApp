namespace Infrastructure.Persistence;

// Реєстр фізичних примірників (інвентарний номер, стан, чи видано). Дочірня таблиця зв'язку 1:N.
// Створюється разом із виданням (AddAsync); звичайний Update її не змінює. Видалення видання каскадне.
public sealed class CopyRow
{
    public int Id { get; set; }
    public string BookId { get; set; } = "";
    public string InventoryNumber { get; set; } = "";
    public string State { get; set; } = "Good";
    public bool IsIssued { get; set; }

    public BookRow Book { get; set; } = null!;
}
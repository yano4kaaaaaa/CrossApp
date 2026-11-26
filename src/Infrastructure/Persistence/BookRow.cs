namespace Infrastructure.Persistence;

// Persistence-модель (рядок таблиці Books). НЕ доменна сутність: публічні сетери і навігація потрібні ORM.
// У домен перетворюється лише через Book.Create (див. EfBookCatalog.ToDomain).
public sealed class BookRow
{
    public string Id { get; set; } = "";
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int Year { get; set; }
    public int Copies { get; set; }                 // кількість примірників (як у контракті API)

    // Ключі пошуку: SQLite порівнює регістр лише для ASCII, тому для кирилиці зберігаємо
    // нормалізований (нижній регістр) текст, і фільтр виконується в SQL, а не в пам'яті.
    public string AuthorKey { get; set; } = "";
    public string SearchKey { get; set; } = "";

    public List<CopyRow> CopyRows { get; set; } = [];   // 1:N
}
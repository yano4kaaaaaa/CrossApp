using Core.Dto;

namespace Core.Domain;

// Додаткове завдання 1 (тиждень 4): бере ImportResult з тижня 3 (DTO + помилки розбору)
// і повертає готові сутності + ОКРЕМИЙ перелік рядків, що не пройшли доменні інваріанти.
// Та сама ідея "дані + помилки", що й ImportResult, але тепер на рівні сутностей.
public static class BookCopyImportAdapter
{
    public static (IReadOnlyList<BookCopy> Items, IReadOnlyList<string> Errors) ToEntities(
        ImportResult<BookCopyDto> importResult)
    {
        var items = new List<BookCopy>();

        // Помилки розбору файлу (тиждень 3) переносимо як є.
        var errors = new List<string>(importResult.Errors);

        foreach (BookCopyDto dto in importResult.Items)
        {
            try
            {
                items.Add(BookCopy.FromDto(dto));
            }
            catch (Exception ex)
            {
                // Помилки порушення інваріантів (тиждень 4) — окремо від помилок розбору.
                errors.Add($"{dto.Id}: {ex.Message}");
            }
        }

        return (items, errors);
    }
}
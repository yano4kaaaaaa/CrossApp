namespace Api.Contracts;

// Вхідний DTO для POST і PUT (PUT повністю замінює ресурс). Правила: Validation/SaveBookRequestValidator.cs
public sealed record SaveBookRequest(
    string Isbn, string Title, string Author, int Year, int Copies);
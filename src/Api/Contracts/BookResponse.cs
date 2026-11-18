namespace Api.Contracts;

// Вихідний DTO: те, що бачить клієнт. Доменний Book у JSON не віддається.
public sealed record BookResponse(
    string Id, string Isbn, string Title, string Author, int Year, int Copies);
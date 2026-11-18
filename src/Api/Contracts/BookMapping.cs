using Core.Domain;

namespace Api.Contracts;

// Мапінг домен -> контракт живе в Api, а не в Core: Core нічого не знає про HTTP-контракт.
public static class BookMapping
{
    public static BookResponse ToResponse(this Book book) =>
        new(book.Id, book.Isbn, book.Title, book.Author, book.Year, book.Copies);
}
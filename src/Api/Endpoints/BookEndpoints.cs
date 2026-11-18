using System.Security.Cryptography;
using Api.Contracts;
using Api.Validation;
using Core.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class BookEndpoints
{
    public static void MapBooks(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/books").WithTags("Books");

        // GET /api/books?page=1&pageSize=20&search=...&author=...  (додаткове завдання 1: пагінація)
        group.MapGet("/", async Task<Results<Ok<PagedResult<BookResponse>>, ValidationProblem>> (
                IBookCatalog catalog, CancellationToken ct,
                int? page, int? pageSize, string? search, string? author) =>
            {
                int p = page ?? 1;
                int size = pageSize ?? 20;

                var errors = new Dictionary<string, string[]>();
                if (p < 1 || p > 1_000_000) errors["page"] = ["page має бути від 1 до 1 000 000."];
                if (size < 1 || size > 100) errors["pageSize"] = ["pageSize має бути від 1 до 100."];
                if (errors.Count > 0)
                    return TypedResults.ValidationProblem(errors);

                BookPage result = await catalog.SearchAsync(author, search, (p - 1) * size, size, ct);
                return TypedResults.Ok(new PagedResult<BookResponse>(
                    result.Items.Select(b => b.ToResponse()).ToList(), result.Total, p, size));
            })
            .WithName("ListBooks")
            .WithSummary("Список видань (пагінація, пошук, фільтр за автором)");

        group.MapGet("/{id}", async Task<Results<Ok<BookResponse>, NotFound>> (
                string id, IBookCatalog catalog, CancellationToken ct) =>
            {
                Book? book = await catalog.GetByIdAsync(id, ct);
                if (book is null)
                    return TypedResults.NotFound();
                return TypedResults.Ok(book.ToResponse());
            })
            .WithName("GetBook")
            .WithSummary("Видання за id");

        group.MapPost("/", async Task<Results<Created<BookResponse>, ValidationProblem>> (
                SaveBookRequest request, IBookCatalog catalog, CancellationToken ct) =>
            {
                string id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
                Book created = Book.Create(id, request.Isbn, request.Title, request.Author, request.Year, request.Copies);

                await catalog.AddAsync(created, ct);   // дубль ISBN -> DomainException -> 409

                return TypedResults.Created($"/api/books/{created.Id}", created.ToResponse());
            })
            .AddEndpointFilter<ValidationFilter<SaveBookRequest>>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("CreateBook")
            .WithSummary("Створити видання");

        group.MapPut("/{id}", async Task<Results<Ok<BookResponse>, NotFound, ValidationProblem>> (
                string id, SaveBookRequest request, IBookCatalog catalog, CancellationToken ct) =>
            {
                if (await catalog.GetByIdAsync(id, ct) is null)
                    return TypedResults.NotFound();

                // PUT повністю замінює стан: створюємо валідне видання з тим самим id.
                Book replacement = Book.Create(id, request.Isbn, request.Title, request.Author, request.Year, request.Copies);
                await catalog.UpdateAsync(replacement, ct);   // зайнятий ISBN -> 409

                return TypedResults.Ok(replacement.ToResponse());
            })
            .AddEndpointFilter<ValidationFilter<SaveBookRequest>>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("ReplaceBook")
            .WithSummary("Замінити видання");

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound>> (
                string id, IBookCatalog catalog, CancellationToken ct) =>
            {
                if (!await catalog.RemoveAsync(id, ct))
                    return TypedResults.NotFound();
                return TypedResults.NoContent();
            })
            .WithName("DeleteBook")
            .WithSummary("Видалити видання");
    }
}
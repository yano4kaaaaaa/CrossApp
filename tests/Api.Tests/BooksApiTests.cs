using System.Net;
using System.Net.Http.Json;
using System.Text;
using Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

// Додаткове завдання 4: інтеграційні тести на WebApplicationFactory.
// Кожен тест має власну фабрику, тобто власний порожній Singleton-каталог: тести ізольовані.
public class BooksApiTests
{
    private static SaveBookRequest Req(string isbn = "978-617-12-0001-1", string title = "Кобзар",
        string author = "Тарас Шевченко", int year = 1840, int copies = 3) =>
        new(isbn, title, author, year, copies);

    [Fact]
    public async Task Post_Valid_Returns201_WithLocation_AndGetByLocationReturns200()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/books", Req());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var body = await created.Content.ReadFromJsonAsync<BookResponse>();
        Assert.Equal("Кобзар", body!.Title);

        HttpResponseMessage read = await client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(body.Id, (await read.Content.ReadFromJsonAsync<BookResponse>())!.Id);
    }

    [Fact]
    public async Task Post_Invalid_Returns400_WithFieldErrors()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/books", new SaveBookRequest("", "X", "", 1000, -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("Isbn", problem!.Errors.Keys);
        Assert.Contains("Author", problem.Errors.Keys);
        Assert.Contains("Year", problem.Errors.Keys);
        Assert.Contains("Copies", problem.Errors.Keys);
        Assert.DoesNotContain("Exception", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_EmptyBody_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        var response = await client.PostAsync("/api/books",
            new StringContent("", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WrongFieldType_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        var response = await client.PostAsync("/api/books", new StringContent(
            """{"isbn":"1","title":"T","author":"A","year":2000,"copies":"abc"}""",
            Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        var response = await client.GetAsync("/api/books/no-such-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Valid_Returns200_AndReplacesState()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/books", Req()))
            .Content.ReadFromJsonAsync<BookResponse>();

        var response = await client.PutAsJsonAsync($"/api/books/{created!.Id}", Req(title: "Нова назва", copies: 9));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<BookResponse>();
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Нова назва", updated.Title);
        Assert.Equal(9, updated.Copies);
    }

    [Fact]
    public async Task Put_EmptyTitle_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/books", Req()))
            .Content.ReadFromJsonAsync<BookResponse>();

        var response = await client.PutAsJsonAsync($"/api/books/{created!.Id}", Req(title: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnknownId_Returns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/books/no-such-id", Req());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204_ThenGetReturns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/books", Req()))
            .Content.ReadFromJsonAsync<BookResponse>();

        var deleted = await client.DeleteAsync($"/api/books/{created!.Id}");
        var after = await client.GetAsync($"/api/books/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateIsbn_Returns409_AsProblemDetails()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/books", Req());

        var response = await client.PostAsJsonAsync("/api/books", Req(title: "Інша"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ISBN", body);
        Assert.DoesNotContain("Exception", body);
    }

    [Fact]
    public async Task List_Paging_ReturnsPageAndTotal()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();
        for (int i = 1; i <= 5; i++)
            await client.PostAsJsonAsync("/api/books", Req(isbn: $"isbn-{i}", title: $"Книга {i}"));

        var page = await client.GetFromJsonAsync<PagedResult<BookResponse>>("/api/books?page=2&pageSize=2");

        Assert.Equal(5, page!.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Page);
    }

    [Fact]
    public async Task List_AuthorFilter_ReturnsOnlyMatching()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/books", Req(isbn: "a1", author: "Тарас Шевченко"));
        await client.PostAsJsonAsync("/api/books", Req(isbn: "a2", author: "Леся Українка"));

        var page = await client.GetFromJsonAsync<PagedResult<BookResponse>>("/api/books?author=шевч");

        Assert.Equal(1, page!.Total);
        Assert.Equal("Тарас Шевченко", page.Items[0].Author);
    }

    [Fact]
    public async Task List_InvalidPaging_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        var response = await client.GetAsync("/api/books?page=0&pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
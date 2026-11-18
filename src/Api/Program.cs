using Api.Contracts;
using Api.Endpoints;
using Api.Infrastructure;
using Api.Validation;
using Core.Abstractions;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();                        // формат помилок RFC 9457
builder.Services.AddExceptionHandler<DomainExceptionHandler>();   // DomainException -> 409

// Кривий JSON / порожнє тіло / "copies":"abc" -> 400 (а не виняток -> 500), і в Development теж.
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);

// Сховище приходить через DI (параметром делегата), а не статично.
// Singleton: один каталог на весь застосунок; реалізація потокобезпечна.
builder.Services.AddSingleton<IBookCatalog, InMemoryBookCatalog>();
builder.Services.AddSingleton<IValidator<SaveBookRequest>, SaveBookRequestValidator>();

var app = builder.Build();

app.UseExceptionHandler();      // необроблений виняток -> 500 ProblemDetails без stack trace
app.UseStatusCodePages();       // порожні 400/404 -> ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar/v1
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapBooks();

app.Run();

// Потрібно, щоб WebApplicationFactory<Program> бачив точку входу в інтеграційних тестах.
public partial class Program { }
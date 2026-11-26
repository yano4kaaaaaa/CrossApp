using Api.Contracts;
using Api.Endpoints;
using Api.Infrastructure;
using Api.Validation;
using Core.Abstractions;
using FluentValidation;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);

// --- Лабораторна 11: SQLite через EF Core ---
// Рядок підключення з конфігурації (ConnectionStrings:AppDb, перекривається змінною ConnectionStrings__AppDb).
string raw = builder.Configuration.GetConnectionString("AppDb") ?? "Data Source=data/app.db";
var csb = new SqliteConnectionStringBuilder(raw);
if (!Path.IsPathRooted(csb.DataSource))
{
    // Відносний шлях завжди від каталогу застосунку, а не від поточного каталогу запуску.
    string full = Path.Combine(builder.Environment.ContentRootPath, csb.DataSource);
    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
    csb.DataSource = full;
}

// DbContext не потокобезпечний, тому Scoped: окремий екземпляр на кожен HTTP-запит.
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(csb.ConnectionString));
builder.Services.AddScoped<IBookCatalog, EfBookCatalog>();   // було: AddSingleton<IBookCatalog, InMemoryBookCatalog>()
builder.Services.AddSingleton<IValidator<SaveBookRequest>, SaveBookRequestValidator>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // У Development схема створюється/оновлюється міграціями автоматично; у Production командою dotnet ef.
    using (IServiceScope scope = app.Services.CreateScope())
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapBooks();

app.Run();

public partial class Program { }
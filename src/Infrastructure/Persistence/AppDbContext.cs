using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BookRow> Books => Set<BookRow>();
    public DbSet<CopyRow> Copies => Set<CopyRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var book = modelBuilder.Entity<BookRow>();
        book.HasKey(b => b.Id);
        book.Property(b => b.Id).HasMaxLength(32);
        // NOCASE: порівняння ISBN без урахування регістру (ISBN-10 може мати 'X'/'x'), у т.ч. для унікального індексу.
        book.Property(b => b.Isbn).IsRequired().HasMaxLength(20).UseCollation("NOCASE");
        book.Property(b => b.Title).IsRequired().HasMaxLength(200);
        book.Property(b => b.Author).IsRequired().HasMaxLength(200);
        book.Property(b => b.Year).IsRequired();
        book.Property(b => b.Copies).IsRequired();
        book.Property(b => b.AuthorKey).IsRequired().HasMaxLength(200);
        book.Property(b => b.SearchKey).IsRequired().HasMaxLength(512);
        book.HasIndex(b => b.Isbn).IsUnique();

        // Зв'язок 1:N: одне видання, багато примірників; видалення видання каскадно видаляє примірники.
        book.HasMany(b => b.CopyRows).WithOne(c => c.Book)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        var copy = modelBuilder.Entity<CopyRow>();
        copy.HasKey(c => c.Id);
        copy.Property(c => c.InventoryNumber).IsRequired().HasMaxLength(40);
        copy.Property(c => c.State).IsRequired().HasMaxLength(20);
        copy.Property(c => c.IsIssued).IsRequired();
        copy.HasIndex(c => c.InventoryNumber).IsUnique();
    }
}
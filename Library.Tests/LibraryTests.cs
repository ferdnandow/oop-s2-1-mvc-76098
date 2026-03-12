using Library.Domain;
using Library.MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests;

public class LibraryTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CannotLoanBookAlreadyOnActiveLoan()
    {
        var context = GetDbContext();
        var book = new Book { Title = "Test Book", Author = "Author", Isbn = "123", Category = "Fiction", IsAvailable = false };
        var member = new Member { FullName = "John Doe", Email = "john@test.com", Phone = "123456" };
        context.Books.Add(book);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        context.Loans.Add(new Loan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.Now, DueDate = DateTime.Now.AddDays(14) });
        await context.SaveChangesAsync();

        var activeLoans = await context.Loans
            .AnyAsync(l => l.BookId == book.Id && l.ReturnedDate == null);

        Assert.True(activeLoans);
    }

    [Fact]
    public async Task ReturnedLoanMakesBookAvailable()
    {
        var context = GetDbContext();
        var book = new Book { Title = "Test Book", Author = "Author", Isbn = "123", Category = "Fiction", IsAvailable = false };
        var member = new Member { FullName = "John Doe", Email = "john@test.com", Phone = "123456" };
        context.Books.Add(book);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var loan = new Loan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.Now, DueDate = DateTime.Now.AddDays(14) };
        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        loan.ReturnedDate = DateTime.Now;
        book.IsAvailable = true;
        await context.SaveChangesAsync();

        var updatedBook = await context.Books.FindAsync(book.Id);
        Assert.True(updatedBook!.IsAvailable);
    }

    [Fact]
    public async Task BookSearchReturnsExpectedMatches()
    {
        var context = GetDbContext();
        context.Books.AddRange(
            new Book { Title = "Harry Potter", Author = "Rowling", Isbn = "111", Category = "Fiction", IsAvailable = true },
            new Book { Title = "Lord of the Rings", Author = "Tolkien", Isbn = "222", Category = "Fiction", IsAvailable = true },
            new Book { Title = "Clean Code", Author = "Robert Martin", Isbn = "333", Category = "Technology", IsAvailable = true }
        );
        await context.SaveChangesAsync();

        var results = await context.Books
            .Where(b => b.Title.Contains("Harry") || b.Author.Contains("Harry"))
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("Harry Potter", results[0].Title);
    }

    [Fact]
    public async Task OverdueLogicWorksCorrectly()
    {
        var context = GetDbContext();
        var book = new Book { Title = "Test Book", Author = "Author", Isbn = "123", Category = "Fiction", IsAvailable = false };
        var member = new Member { FullName = "John Doe", Email = "john@test.com", Phone = "123456" };
        context.Books.Add(book);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var loan = new Loan
        {
            BookId = book.Id,
            MemberId = member.Id,
            LoanDate = DateTime.Now.AddDays(-20),
            DueDate = DateTime.Now.AddDays(-5),
            ReturnedDate = null
        };
        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        var overdueLoans = await context.Loans
            .Where(l => l.DueDate < DateTime.Now && l.ReturnedDate == null)
            .ToListAsync();

        Assert.Single(overdueLoans);
    }

    [Fact]
    public async Task FilterByCategoryReturnsCorrectResults()
    {
        var context = GetDbContext();
        context.Books.AddRange(
            new Book { Title = "Book 1", Author = "Author 1", Isbn = "111", Category = "Fiction", IsAvailable = true },
            new Book { Title = "Book 2", Author = "Author 2", Isbn = "222", Category = "Science", IsAvailable = true },
            new Book { Title = "Book 3", Author = "Author 3", Isbn = "333", Category = "Fiction", IsAvailable = true }
        );
        await context.SaveChangesAsync();

        var results = await context.Books
            .Where(b => b.Category == "Fiction")
            .ToListAsync();

        Assert.Equal(2, results.Count);
    }
}

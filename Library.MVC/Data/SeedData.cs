using Bogus;
using Library.Domain;
using Microsoft.AspNetCore.Identity;

namespace Library.MVC.Data;

public static class SeedData
{
    public static async Task InitialiseAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed Admin role
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        // Seed Admin user
        if (await userManager.FindByEmailAsync("assignment@dorset.ie") == null)
        {
            var admin = new IdentityUser { UserName = "assignment@dorset.ie", Email = "assignment@dorset.ie" };
            await userManager.CreateAsync(admin, "Dorset123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (context.Books.Any()) return; // already seeded

        // Seed Books
        var bookFaker = new Faker<Book>()
            .RuleFor(b => b.Title, f => f.Lorem.Sentence(3))
            .RuleFor(b => b.Author, f => f.Name.FullName())
            .RuleFor(b => b.Isbn, f => f.Commerce.Ean13())
            .RuleFor(b => b.Category, f => f.PickRandom("Fiction", "Science", "History", "Technology", "Art"))
            .RuleFor(b => b.IsAvailable, _ => true);

        var books = bookFaker.Generate(20);
        context.Books.AddRange(books);

        // Seed Members
        var memberFaker = new Faker<Member>()
            .RuleFor(m => m.FullName, f => f.Name.FullName())
            .RuleFor(m => m.Email, f => f.Internet.Email())
            .RuleFor(m => m.Phone, f => f.Phone.PhoneNumber());

        var members = memberFaker.Generate(10);
        context.Members.AddRange(members);

        await context.SaveChangesAsync();

        // Seed Loans
        var random = new Random();
        var loans = new List<Loan>();

        for (int i = 0; i < 15; i++)
        {
            var book = books[i % books.Count];
            var member = members[random.Next(members.Count)];
            var loanDate = DateTime.Now.AddDays(-random.Next(1, 30));
            var dueDate = loanDate.AddDays(14);

            DateTime? returnedDate = i switch
            {
                < 5 => dueDate.AddDays(-1),        // returned on time
                < 8 => null,                        // active loan
                < 11 => dueDate.AddDays(5),         // returned late
                _ => null                           // overdue (not returned)
            };

            book.IsAvailable = returnedDate != null || i >= 11 ? book.IsAvailable : false;

            loans.Add(new Loan
            {
                Book = book,
                Member = member,
                LoanDate = loanDate,
                DueDate = dueDate,
                ReturnedDate = returnedDate
            });
        }

        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();
    }
}
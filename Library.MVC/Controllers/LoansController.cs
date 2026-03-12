using Library.Domain;
using Library.MVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Library.MVC.Controllers;

public class LoansController : Controller
{
    private readonly AppDbContext _context;

    public LoansController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var loans = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .ToListAsync();
        return View(loans);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Members = new SelectList(await _context.Members.ToListAsync(), "Id", "FullName");
        ViewBag.Books = new SelectList(await _context.Books.Where(b => b.IsAvailable).ToListAsync(), "Id", "Title");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Loan loan)
    {
        // Check if book is already on active loan
        var activeLoad = await _context.Loans
            .AnyAsync(l => l.BookId == loan.BookId && l.ReturnedDate == null);

        if (activeLoad)
        {
            ModelState.AddModelError("", "This book is already on an active loan.");
            ViewBag.Members = new SelectList(await _context.Members.ToListAsync(), "Id", "FullName");
            ViewBag.Books = new SelectList(await _context.Books.Where(b => b.IsAvailable).ToListAsync(), "Id", "Title");
            return View(loan);
        }

        ModelState.Remove("Book");
        ModelState.Remove("Member");
        if (ModelState.IsValid)
        {
            loan.LoanDate = DateTime.Now;
            _context.Loans.Add(loan);

            // Mark book as unavailable
            var book = await _context.Books.FindAsync(loan.BookId);
            if (book != null) book.IsAvailable = false;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Members = new SelectList(await _context.Members.ToListAsync(), "Id", "FullName");
        ViewBag.Books = new SelectList(await _context.Books.Where(b => b.IsAvailable).ToListAsync(), "Id", "Title");
        return View(loan);
    }

    // Mark as returned
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReturned(int id)
    {
        var loan = await _context.Loans.Include(l => l.Book).FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound();

        loan.ReturnedDate = DateTime.Now;
        if (loan.Book != null) loan.Book.IsAvailable = true;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
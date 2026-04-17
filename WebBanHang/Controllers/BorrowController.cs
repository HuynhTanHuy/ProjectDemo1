using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class BorrowController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const int MaxBorrowLimit = 5;
        private const int DefaultBorrowDays = 14;
        private const decimal LateFeePerDay = 5000m;

        public BorrowController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorrowBook(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var book = await _db.Products.FirstOrDefaultAsync(x => x.Id == bookId);
            if (book == null)
            {
                return NotFound();
            }

            var hasUnpaidPenalty = await _db.Penalties.AnyAsync(x => x.UserId == userId && !x.IsPaid);
            if (hasUnpaidPenalty)
            {
                TempData["Error"] = "You still have unpaid penalties. Please settle penalties before borrowing.";
                return RedirectToAction("Details", "Product", new { area = "Customer", id = bookId });
            }

            if (book.Stock <= 0)
            {
                TempData["Error"] = "Book is out of stock.";
                return RedirectToAction("Details", "Product", new { area = "Customer", id = bookId });
            }

            var activeBorrows = await _db.Borrows.CountAsync(x => x.UserId == userId && x.Status == BorrowStatus.Borrowing);
            if (activeBorrows >= MaxBorrowLimit)
            {
                TempData["Error"] = $"You have reached the borrow limit ({MaxBorrowLimit} books).";
                return RedirectToAction("Details", "Product", new { area = "Customer", id = bookId });
            }

            var hasBorrowedSameBook = await _db.Borrows.AnyAsync(x =>
                x.UserId == userId &&
                x.BookId == bookId &&
                x.Status == BorrowStatus.Borrowing);

            if (hasBorrowedSameBook)
            {
                TempData["Error"] = "You are already borrowing this book.";
                return RedirectToAction("Details", "Product", new { area = "Customer", id = bookId });
            }

            book.Stock -= 1;
            _db.Borrows.Add(new Borrow
            {
                UserId = userId,
                BookId = bookId,
                BorrowDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(DefaultBorrowDays),
                Status = BorrowStatus.Borrowing
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Borrow created successfully.";
            return RedirectToAction("MyBorrows");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int borrowId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var borrow = await _db.Borrows
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == borrowId && x.UserId == userId);

            if (borrow == null)
            {
                return NotFound();
            }

            if (borrow.Status == BorrowStatus.Returned)
            {
                TempData["Error"] = "This borrow record was already returned.";
                return RedirectToAction("MyBorrows");
            }

            borrow.ReturnDate = DateTime.UtcNow;
            borrow.Status = BorrowStatus.Returned;
            if (borrow.Book != null)
            {
                borrow.Book.Stock += 1;
            }

            if (borrow.ReturnDate.Value > borrow.DueDate)
            {
                var lateDays = (borrow.ReturnDate.Value.Date - borrow.DueDate.Date).Days;
                _db.Penalties.Add(new Penalty
                {
                    UserId = borrow.UserId,
                    BorrowId = borrow.Id,
                    Amount = lateDays * LateFeePerDay,
                    Reason = $"Late return ({lateDays} day(s)).",
                    CreatedAt = DateTime.UtcNow,
                    IsPaid = false
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Book returned successfully.";
            return RedirectToAction("MyBorrows");
        }

        [HttpGet]
        public async Task<IActionResult> MyBorrows()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var borrows = await _db.Borrows
                .AsNoTracking()
                .Include(x => x.Book)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync();

            return View(borrows);
        }
    }
}

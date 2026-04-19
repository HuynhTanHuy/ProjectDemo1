using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Helpers
{
    public record BookCatalogQuery(
        int? GenreId,
        string? Status,
        int? CategoryId,
        int? AuthorId,
        int? PublisherId,
        string? Search,
        int Page,
        int PageSize,
        int? MinRating = null,
        int? YearFrom = null,
        int? YearTo = null,
        string? Lang = null,
        string? Sort = null);

    public static class BookCatalogHelper
    {
        public static async Task<ProductBooksPageViewModel> BuildAsync(
            ApplicationDbContext db,
            BookCatalogQuery query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Max(1, query.PageSize);
            var statusNorm = string.IsNullOrWhiteSpace(query.Status)
                ? null
                : query.Status.Trim().ToLowerInvariant();

            var vm = new ProductBooksPageViewModel
            {
                GenreId = query.GenreId,
                Status = statusNorm
            };

            vm.GenreOptions = await db.Genres
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new SelectListItem { Text = g.Name, Value = g.Id.ToString() })
                .ToListAsync();
            vm.GenreOptions.Insert(0, new SelectListItem { Value = "", Text = "Tất cả thể loại" });

            var overdueByBook = (await db.Borrows
                    .AsNoTracking()
                    .Where(b => b.Status == BorrowStatus.Borrowing && b.DueDate.Date < DateTime.UtcNow.Date)
                    .GroupBy(b => b.BookId)
                    .Select(g => new { BookId = g.Key, Cnt = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.BookId, x => x.Cnt);

            var borrowingByBook = (await db.Borrows
                    .AsNoTracking()
                    .Where(b => b.Status == BorrowStatus.Borrowing && b.DueDate.Date >= DateTime.UtcNow.Date)
                    .GroupBy(b => b.BookId)
                    .Select(g => new { BookId = g.Key, Cnt = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.BookId, x => x.Cnt);

            // Không Include Reviews ở đây: JOIN tập Review có thể làm kết quả lọc/tìm sai trên SQL Server.
            IQueryable<Product> q = db.Products
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Genre)
                .Include(p => p.Category)
                .Include(p => p.Publisher);

            if (query.GenreId is > 0)
            {
                q = q.Where(p => p.GenreId == query.GenreId);
            }

            if (query.CategoryId is > 0)
            {
                q = q.Where(p => p.CategoryId == query.CategoryId);
            }

            if (query.AuthorId is > 0)
            {
                q = q.Where(p => p.AuthorId == query.AuthorId);
            }

            if (query.PublisherId is > 0)
            {
                q = q.Where(p => p.PublisherId == query.PublisherId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var ph = query.Search.Trim().ToLowerInvariant();
                q = q.Where(p =>
                    p.Name.ToLower().Contains(ph) ||
                    p.Description.ToLower().Contains(ph) ||
                    (p.Isbn != null && p.Isbn.ToLower().Contains(ph)) ||
                    (p.Slug != null && p.Slug.ToLower().Contains(ph)) ||
                    (p.Language != null && p.Language.ToLower().Contains(ph)) ||
                    (p.Author != null && p.Author.Name.ToLower().Contains(ph)) ||
                    (p.Genre != null && p.Genre.Name.ToLower().Contains(ph)) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(ph)) ||
                    (p.Publisher != null && p.Publisher.Name.ToLower().Contains(ph)));
            }

            var products = await q.OrderBy(p => p.Name).ToListAsync();

            Dictionary<int, double> avgByProduct = new();
            if (products.Count > 0)
            {
                var ids = products.Select(p => p.Id).ToList();
                avgByProduct = await db.Reviews
                    .AsNoTracking()
                    .Where(r => ids.Contains(r.ProductId))
                    .GroupBy(r => r.ProductId)
                    .Select(g => new { g.Key, Avg = g.Average(r => (double)r.Rating) })
                    .ToDictionaryAsync(x => x.Key, x => x.Avg);
            }

            var filtered = new List<BookViewModel>();
            foreach (var p in products)
            {
                borrowingByBook.TryGetValue(p.Id, out var borrowing);
                overdueByBook.TryGetValue(p.Id, out var overdue);
                // Stock = số bản còn tại shop (mượn đi thì đã trừ). Trạng thái hiển thị ưu tiên tồn kho thực tế.
                string statusKey;
                string statusLabel;
                if (p.Stock > 0)
                {
                    statusKey = "available";
                    statusLabel = "Sẵn có";
                }
                else if (overdue > 0)
                {
                    statusKey = "overdue";
                    statusLabel = "Quá hạn";
                }
                else if (borrowing > 0)
                {
                    statusKey = "borrowing";
                    statusLabel = "Đang mượn";
                }
                else
                {
                    statusKey = "out";
                    statusLabel = "Hết sách";
                }

                if (!string.IsNullOrEmpty(vm.Status))
                {
                    if (vm.Status == "available" && statusKey != "available") continue;
                    if (vm.Status == "borrowing" && statusKey != "borrowing") continue;
                    if (vm.Status == "overdue" && statusKey != "overdue") continue;
                    if (vm.Status == "out" && statusKey != "out") continue;
                }

                var avg = avgByProduct.TryGetValue(p.Id, out var av) ? av : 0d;
                if (query.MinRating is > 0 && avg < query.MinRating.Value - 0.001)
                {
                    continue;
                }

                var pubYear = p.PublicationDate?.Year;
                if (query.YearFrom is > 0 && (pubYear == null || pubYear < query.YearFrom))
                {
                    continue;
                }

                if (query.YearTo is > 0 && (pubYear == null || pubYear > query.YearTo))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(query.Lang))
                {
                    if (string.IsNullOrEmpty(p.Language) ||
                        p.Language.IndexOf(query.Lang.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                filtered.Add(new BookViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    Author = p.Author?.Name ?? "—",
                    Category = p.Category?.Name ?? "—",
                    Genre = p.Genre?.Name ?? "—",
                    Isbn = p.Isbn,
                    Quantity = p.Stock,
                    StatusKey = statusKey,
                    StatusLabel = statusLabel,
                    AvgRating = avg,
                    PublicationYear = pubYear,
                    Language = p.Language,
                    PublishedAt = p.PublicationDate
                });
            }

            var sortNorm = string.IsNullOrWhiteSpace(query.Sort) ? "popular" : query.Sort.Trim().ToLowerInvariant();
            IEnumerable<BookViewModel> ordered = sortNorm switch
            {
                "newest" => filtered.OrderByDescending(b => b.PublishedAt ?? DateTime.MinValue).ThenBy(b => b.Name),
                "az" => filtered.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase),
                _ => filtered
                    .OrderByDescending(b => b.AvgRating)
                    .ThenByDescending(b => b.Quantity)
                    .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            };

            var orderedList = ordered.ToList();
            var totalFiltered = orderedList.Count;
            vm.Books = orderedList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            vm.StatTotalBooks = await db.Products.CountAsync();
            vm.StatBorrowing = await db.Borrows.CountAsync(b => b.Status == BorrowStatus.Borrowing);
            vm.StatAvailableTitles = await db.Products.CountAsync(p => p.Stock > 0);
            vm.StatOverdueLoans = await db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Borrowing && b.DueDate.Date < DateTime.UtcNow.Date);

            foreach (var o in vm.GenreOptions)
            {
                o.Selected = string.IsNullOrEmpty(o.Value)
                    ? !vm.GenreId.HasValue || vm.GenreId == 0
                    : o.Value == vm.GenreId?.ToString();
            }

            vm.TotalFilteredCount = totalFiltered;
            vm.PageNumber = page;
            vm.PageSize = pageSize;
            return vm;
        }

    }
}

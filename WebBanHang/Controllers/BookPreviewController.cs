using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Controllers
{
    [Route("[controller]")]
    public class BookPreviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public BookPreviewController(ApplicationDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpGet("View/{bookId:int}")]
        public async Task<IActionResult> View(int bookId)
        {
            var preview = await _db.BookPreviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BookId == bookId);

            if (preview == null)
            {
                ViewBag.BookId = bookId;
                return base.View("NoPreview");
            }

            var vm = new BookPreviewViewModel
            {
                Id = preview.Id,
                BookId = preview.BookId,
                PreviewType = preview.PreviewType,
                ExistingFilePath = preview.FilePath,
                Content = preview.Content,
                TotalPages = preview.TotalPages,
                PreviewPages = preview.PreviewPages,
                AllowDownload = preview.AllowDownload,
                CreatedAt = preview.CreatedAt
            };

            return base.View(vm);
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpGet("Manage/{bookId:int}")]
        public async Task<IActionResult> Manage(int bookId)
        {
            var preview = await _db.BookPreviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BookId == bookId);

            var vm = new BookPreviewViewModel
            {
                BookId = bookId,
                Books = await GetBookSelectListAsync()
            };

            if (preview != null)
            {
                vm.Id = preview.Id;
                vm.PreviewType = preview.PreviewType;
                vm.ExistingFilePath = preview.FilePath;
                vm.Content = preview.Content;
                vm.TotalPages = preview.TotalPages;
                vm.PreviewPages = preview.PreviewPages;
                vm.AllowDownload = preview.AllowDownload;
                vm.CreatedAt = preview.CreatedAt;
            }

            return View(vm);
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookPreviewViewModel model)
        {
            if (model.PreviewPages > model.TotalPages)
            {
                ModelState.AddModelError(nameof(model.PreviewPages), "Preview pages must be less than or equal to total pages.");
            }
            AddPayloadValidationErrors(model);

            if (!ModelState.IsValid)
            {
                model.Books = await GetBookSelectListAsync();
                return View("Manage", model);
            }

            var existing = await _db.BookPreviews.FirstOrDefaultAsync(x => x.BookId == model.BookId);
            if (existing != null)
            {
                return BadRequest("Preview for this book already exists. Please use Update.");
            }

            var preview = new BookPreview
            {
                BookId = model.BookId,
                PreviewType = model.PreviewType,
                TotalPages = model.TotalPages,
                PreviewPages = model.PreviewPages,
                AllowDownload = model.AllowDownload,
                CreatedAt = DateTime.UtcNow
            };

            await PopulatePreviewPayloadAsync(preview, model);

            _db.BookPreviews.Add(preview);
            await _db.SaveChangesAsync();

            TempData["success"] = "Book preview created successfully.";
            return RedirectToAction(nameof(Manage), new { bookId = model.BookId });
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(BookPreviewViewModel model)
        {
            if (model.PreviewPages > model.TotalPages)
            {
                ModelState.AddModelError(nameof(model.PreviewPages), "Preview pages must be less than or equal to total pages.");
            }
            AddPayloadValidationErrors(model);

            if (!ModelState.IsValid)
            {
                model.Books = await GetBookSelectListAsync();
                return View("Manage", model);
            }

            var preview = await _db.BookPreviews.FirstOrDefaultAsync(x => x.BookId == model.BookId);
            if (preview == null)
            {
                return NotFound();
            }

            preview.PreviewType = model.PreviewType;
            preview.TotalPages = model.TotalPages;
            preview.PreviewPages = model.PreviewPages;
            preview.AllowDownload = model.AllowDownload;

            await PopulatePreviewPayloadAsync(preview, model);

            await _db.SaveChangesAsync();
            TempData["success"] = "Book preview updated successfully.";

            return RedirectToAction(nameof(Manage), new { bookId = model.BookId });
        }

        [HttpGet("Stream/{bookId:int}")]
        public async Task<IActionResult> Stream(int bookId)
        {
            var preview = await _db.BookPreviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BookId == bookId);

            if (preview == null)
            {
                return NotFound();
            }

            if (preview.PreviewType == PreviewType.Pdf)
            {
                if (string.IsNullOrWhiteSpace(preview.FilePath))
                {
                    return NotFound();
                }

                var physicalPath = Path.Combine(_environment.WebRootPath, preview.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(physicalPath))
                {
                    return NotFound();
                }

                return PhysicalFile(physicalPath, "application/pdf", enableRangeProcessing: true);
            }

            return Content(BuildLimitedTextContent(preview.Content, preview.PreviewPages), "text/html");
        }

        [HttpPost("Log")]
        public async Task<IActionResult> Log([FromForm] int bookId, [FromForm] int durationSeconds)
        {
            var hasPreview = await _db.BookPreviews.AnyAsync(x => x.BookId == bookId);
            if (!hasPreview)
            {
                return NotFound();
            }

            var userId = User?.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;
            var log = new UserPreviewLog
            {
                BookId = bookId,
                UserId = userId,
                DurationSeconds = Math.Max(0, durationSeconds),
                ViewedAt = DateTime.UtcNow
            };

            _db.UserPreviewLogs.Add(log);
            await _db.SaveChangesAsync();

            return Ok();
        }

        private async Task PopulatePreviewPayloadAsync(BookPreview preview, BookPreviewViewModel model)
        {
            preview.Content = null;
            preview.FilePath = null;

            if (model.PreviewType == PreviewType.Pdf)
            {
                if (model.File == null || model.File.Length == 0)
                {
                    if (string.IsNullOrWhiteSpace(model.ExistingFilePath))
                    {
                        throw new InvalidOperationException("PDF file is required for PDF preview.");
                    }

                    preview.FilePath = model.ExistingFilePath;
                    return;
                }

                var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "previews");
                Directory.CreateDirectory(uploadsRoot);
                var generatedFileName = await SaveLimitedPdfAsync(model.File, model.PreviewPages, uploadsRoot);
                preview.FilePath = $"/uploads/previews/{generatedFileName}";
            }
            else
            {
                preview.Content = BuildLimitedTextContent(model.Content, model.PreviewPages);
            }
        }

        private async Task<IEnumerable<SelectListItem>> GetBookSelectListAsync()
        {
            return await _db.Products
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToListAsync();
        }

        private void AddPayloadValidationErrors(BookPreviewViewModel model)
        {
            if (model.PreviewType == PreviewType.Pdf && model.File == null && string.IsNullOrWhiteSpace(model.ExistingFilePath))
            {
                ModelState.AddModelError(nameof(model.File), "PDF file is required for PDF preview.");
            }

            if (model.PreviewType == PreviewType.Text && string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Content is required for text preview.");
            }
        }

        private static string BuildLimitedTextContent(string? rawContent, int previewPages)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return string.Empty;
            }

            var normalizedPages = Math.Max(1, previewPages);
            const string pageBreakToken = "<!--pagebreak-->";

            if (rawContent.Contains(pageBreakToken, StringComparison.OrdinalIgnoreCase))
            {
                var pages = rawContent.Split(pageBreakToken, StringSplitOptions.None);
                return string.Join(pageBreakToken, pages.Take(normalizedPages));
            }

            var maxLength = normalizedPages * 2000;
            if (rawContent.Length <= maxLength)
            {
                return rawContent;
            }

            return $"{rawContent[..maxLength]}<p><em>... Preview truncated ...</em></p>";
        }

        private static async Task<string> SaveLimitedPdfAsync(IFormFile uploadedFile, int previewPages, string uploadsRoot)
        {
            await using var inputStream = uploadedFile.OpenReadStream();
            using var sourceDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
            using var previewDocument = new PdfDocument();

            var pagesToTake = Math.Min(Math.Max(1, previewPages), sourceDocument.PageCount);
            for (var i = 0; i < pagesToTake; i++)
            {
                previewDocument.AddPage(sourceDocument.Pages[i]);
            }

            var fileName = $"{Guid.NewGuid()}.pdf";
            var outputPath = Path.Combine(uploadsRoot, fileName);
            await using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            previewDocument.Save(outputStream, false);

            return fileName;
        }
    }
}

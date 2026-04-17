using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebBanHang.Models.ViewModels
{
    public class BookPreviewViewModel : IValidatableObject
    {
        public Guid? Id { get; set; }

        [Required]
        [Display(Name = "Book")]
        public int BookId { get; set; }

        [Required]
        [Display(Name = "Preview type")]
        public PreviewType PreviewType { get; set; } = PreviewType.Pdf;

        public string? ExistingFilePath { get; set; }

        [Display(Name = "PDF file")]
        public IFormFile? File { get; set; }

        [Display(Name = "HTML content")]
        public string? Content { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Total pages")]
        public int TotalPages { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Preview pages")]
        public int PreviewPages { get; set; }

        [Display(Name = "Allow download")]
        public bool AllowDownload { get; set; }

        public DateTime CreatedAt { get; set; }

        public IEnumerable<SelectListItem> Books { get; set; } = Enumerable.Empty<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PreviewPages > TotalPages)
            {
                yield return new ValidationResult(
                    "Preview pages must be less than or equal to total pages.",
                    new[] { nameof(PreviewPages), nameof(TotalPages) });
            }

            if (PreviewType == PreviewType.Pdf && File == null && string.IsNullOrWhiteSpace(ExistingFilePath))
            {
                yield return new ValidationResult(
                    "PDF file is required for PDF preview type.",
                    new[] { nameof(File) });
            }

            if (PreviewType == PreviewType.Text && string.IsNullOrWhiteSpace(Content))
            {
                yield return new ValidationResult(
                    "Content is required for text preview type.",
                    new[] { nameof(Content) });
            }
        }
    }
}

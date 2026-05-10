using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebBanHang.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public
       ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<BookPreview> BookPreviews { get; set; }
        public DbSet<UserPreviewLog> UserPreviewLogs { get; set; }
        public DbSet<Borrow> Borrows { get; set; }
        public DbSet<Penalty> Penalties { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<BookCopy> BookCopies { get; set; }
        public DbSet<BookInventorySession> BookInventorySessions { get; set; }
        public DbSet<BookInventoryScan> BookInventoryScans { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<BookPreview>()
                .HasIndex(x => x.BookId);

            builder.Entity<BookPreview>()
                .HasOne(x => x.Book)
                .WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPreviewLog>()
                .HasIndex(x => x.UserId);

            builder.Entity<UserPreviewLog>()
                .HasIndex(x => x.BookId);

            builder.Entity<UserPreviewLog>()
                .HasOne(x => x.Book)
                .WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPreviewLog>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Borrow>()
                .HasIndex(x => x.UserId);

            builder.Entity<Borrow>()
                .HasIndex(x => x.BookId);

            builder.Entity<Borrow>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Borrow>()
                .HasOne(x => x.Book)
                .WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Borrow>()
                .HasIndex(x => x.BookCopyId);

            builder.Entity<Borrow>()
                .HasOne(x => x.BookCopy)
                .WithMany()
                .HasForeignKey(x => x.BookCopyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<BookCopy>()
                .HasIndex(x => x.CopyCode)
                .IsUnique();

            builder.Entity<BookCopy>()
                .HasIndex(x => x.ProductId);

            builder.Entity<BookCopy>()
                .HasOne(x => x.Book)
                .WithMany(x => x.BookCopies)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationUser>()
                .HasIndex(x => x.LibraryMemberQrToken)
                .IsUnique()
                .HasFilter("[LibraryMemberQrToken] IS NOT NULL");

            builder.Entity<BookInventorySession>()
                .HasIndex(x => x.StartedByUserId);

            builder.Entity<BookInventorySession>()
                .HasOne(x => x.StartedBy)
                .WithMany()
                .HasForeignKey(x => x.StartedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<BookInventoryScan>()
                .HasIndex(x => new { x.SessionId, x.BookCopyId })
                .IsUnique();

            builder.Entity<BookInventoryScan>()
                .HasOne(x => x.Session)
                .WithMany(x => x.Scans)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BookInventoryScan>()
                .HasOne(x => x.BookCopy)
                .WithMany()
                .HasForeignKey(x => x.BookCopyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Penalty>()
                .HasIndex(x => x.UserId);

            builder.Entity<Penalty>()
                .HasIndex(x => x.BorrowId);

            builder.Entity<Penalty>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Penalty>()
                .HasOne(x => x.Borrow)
                .WithMany()
                .HasForeignKey(x => x.BorrowId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(x => x.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderDetail>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            builder.Entity<Penalty>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Entity<SystemSetting>()
                .HasKey(x => x.Id);

            builder.Entity<SystemSetting>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Entity<SystemSetting>()
                .Property(x => x.BorrowFee)
                .HasPrecision(18, 2);

            builder.Entity<SystemSetting>()
                .Property(x => x.OverdueFeePerDay)
                .HasPrecision(18, 2);

            builder.Entity<Borrow>()
                .Property(x => x.BorrowFeeAmount)
                .HasPrecision(18, 2);

            builder.Entity<Borrow>()
                .Property(x => x.FineAmount)
                .HasPrecision(18, 2);

            builder.Entity<Payment>()
                .HasIndex(x => x.TransactionCode)
                .IsUnique();

            builder.Entity<Payment>()
                .HasIndex(x => x.IdempotencyKey)
                .IsUnique();

            builder.Entity<Payment>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Payment>()
                .HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PaymentTransaction>()
                .HasIndex(x => x.PaymentId);

            builder.Entity<PaymentTransaction>()
                .HasOne(x => x.Payment)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

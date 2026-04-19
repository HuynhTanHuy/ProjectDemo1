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
        }
    }
}

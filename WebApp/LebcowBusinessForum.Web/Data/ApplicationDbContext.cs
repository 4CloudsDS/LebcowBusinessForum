using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LebcowBusinessForum.Web.Models;

namespace LebcowBusinessForum.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }


    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<BusinessEvent> Events => Set<BusinessEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {

        builder.HasDefaultSchema("BusinessForums");

        base.OnModelCreating(builder);
        

        // ── Rename Identity tables to match project conventions ─────────────
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // ── ApplicationUser ─────────────────────────────────────────────────
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
        });

        // ── Category ────────────────────────────────────────────────────────
        builder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();

            // Self-referential: parent → children
            e.HasOne(c => c.ParentCategory)
             .WithMany(c => c.SubCategories)
             .HasForeignKey(c => c.ParentCategoryId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Business ────────────────────────────────────────────────────────
        builder.Entity<Business>(e =>
        {
            e.HasKey(b => b.BusinessId);
            e.Property(b => b.Name).HasMaxLength(200).IsRequired();
            e.Property(b => b.Address).HasMaxLength(500).IsRequired();
            e.Property(b => b.Phone).HasMaxLength(30);
            e.Property(b => b.Email).HasMaxLength(254);
            e.Property(b => b.Website).HasMaxLength(500);
            e.Property(b => b.Description).HasMaxLength(4000);
            e.Property(b => b.LogoUrl).HasMaxLength(500);
            e.Property(b => b.Status).HasMaxLength(20).HasDefaultValue("pending");
            e.Property(b => b.Region).HasMaxLength(100);

            // Category FK
            e.HasOne(b => b.Category)
             .WithMany(c => c.Businesses)
             .HasForeignKey(b => b.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            // Owner FK
            e.HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey(b => b.OwnerId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            e.HasIndex(b => b.Status);
            e.HasIndex(b => b.CategoryId);
            e.HasIndex(b => b.OwnerId);
            e.HasIndex(b => b.Region);

            // Full-text search index (SQL Server)
            e.HasIndex(b => new { b.Name, b.Description })
             .HasDatabaseName("IX_Businesses_FullText")
             .IsUnique(false);
        });

        // ── Listing ─────────────────────────────────────────────────────────
        builder.Entity<Listing>(e =>
        {
            e.HasKey(l => l.ListingId);
            e.Property(l => l.Tier).HasMaxLength(20).HasDefaultValue("free");
            e.Property(l => l.PaymentStatus).HasMaxLength(30).HasDefaultValue("unpaid");

            e.HasOne(l => l.Business)
             .WithMany(b => b.Listings)
             .HasForeignKey(l => l.BusinessId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(l => l.BusinessId);
            e.HasIndex(l => l.Tier);
            e.HasIndex(l => l.EndDate);
        });

        // ── Review ───────────────────────────────────────────────────────────
        builder.Entity<Review>(e =>
        {
            e.HasKey(r => r.ReviewId);
            e.Property(r => r.Comment).HasMaxLength(2000);
            e.Property(r => r.Rating).IsRequired();

            e.HasOne(r => r.Business)
             .WithMany(b => b.Reviews)
             .HasForeignKey(r => r.BusinessId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            // One review per user per business
            e.HasIndex(r => new { r.BusinessId, r.UserId }).IsUnique();
            e.HasIndex(r => r.BusinessId);
        });

        // ── BusinessEvent ────────────────────────────────────────────────────
        builder.Entity<BusinessEvent>(e =>
        {
            e.HasKey(ev => ev.EventId);
            e.Property(ev => ev.Title).HasMaxLength(200).IsRequired();
            e.Property(ev => ev.Description).HasMaxLength(4000);
            e.Property(ev => ev.Location).HasMaxLength(500);

            e.HasOne(ev => ev.Organizer)
             .WithMany()
             .HasForeignKey(ev => ev.OrganizerId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(ev => ev.Date);
            e.HasIndex(ev => ev.OrganizerId);
        });

        // ── AuditLog ─────────────────────────────────────────────────────────
        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.LogId);
            e.Property(a => a.Action).HasMaxLength(500).IsRequired();

            e.HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey("UserId")
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.Timestamp);
        });

        // ── UserFavorite (many-to-many join) ─────────────────────────────────
        builder.Entity<UserFavorite>(e =>
        {
            e.HasKey(uf => new { uf.UserId, uf.BusinessId });

            e.HasOne(uf => uf.User)
             .WithMany()
             .HasForeignKey(uf => uf.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(uf => uf.Business)
             .WithMany(b => b.Favorites)
             .HasForeignKey(uf => uf.BusinessId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(uf => uf.UserId);
            e.HasIndex(uf => uf.BusinessId);
        });

        // ── ForumPost ─────────────────────────────────────────────────────────
        builder.Entity<ForumPost>(e =>
        {
            e.HasKey(p => p.PostId);
            e.Property(p => p.Title).HasMaxLength(200).IsRequired();
            e.Property(p => p.Body).HasMaxLength(10000).IsRequired();

            e.HasOne(p => p.Author)
             .WithMany()
             .HasForeignKey(p => p.AuthorId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(p => p.CreatedAt);
            e.HasIndex(p => p.AuthorId);
        });

        // ── NewsletterSubscription ────────────────────────────────────────────
        builder.Entity<NewsletterSubscription>(e =>
        {
            e.HasKey(n => n.SubscriptionId);
            e.Property(n => n.Email).HasMaxLength(254).IsRequired();
            e.HasIndex(n => n.Email).IsUnique();
        });
    }
}


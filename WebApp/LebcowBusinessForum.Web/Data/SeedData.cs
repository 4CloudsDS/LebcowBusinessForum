using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // ── Roles ───────────────────────────────────────────────
            string[] roles = { "Admin", "BusinessOwner", "User" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = roleName, NormalizedName = roleName.ToUpper() });
                }
            }

            // ── Users ───────────────────────────────────────────────
            var admin = new ApplicationUser { UserName = "admin@forum.co.za", Email = "admin@forum.co.za", FullName = "Admin User", CreatedAt = DateTime.UtcNow, IsActive = true };
            var thabo = new ApplicationUser { UserName = "thabo@forum.co.za", Email = "thabo@forum.co.za", FullName = "Thabo Mokoena", CreatedAt = DateTime.UtcNow, IsActive = true };
            var naledi = new ApplicationUser { UserName = "naledi@forum.co.za", Email = "naledi@forum.co.za", FullName = "Naledi Khumalo", CreatedAt = DateTime.UtcNow, IsActive = true };
            var sipho = new ApplicationUser { UserName = "sipho@forum.co.za", Email = "sipho@forum.co.za", FullName = "Sipho Dlamini", CreatedAt = DateTime.UtcNow, IsActive = true };
            var reabetswe = new ApplicationUser { UserName = "reabetswe@forum.co.za", Email = "reabetswe@forum.co.za", FullName = "Reabetswe Mogoswane", CreatedAt = DateTime.UtcNow, IsActive = true };

            async Task EnsureUser(ApplicationUser user, string password, string role)
            {
                var existing = await userManager.FindByEmailAsync(user.Email);
                if (existing == null)
                {
                    await userManager.CreateAsync(user, password);
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            await EnsureUser(admin, "Admin123!", "Admin");
            await EnsureUser(thabo, "Owner123!", "BusinessOwner");
            await EnsureUser(naledi, "Owner123!", "BusinessOwner");
            await EnsureUser(sipho, "User123!", "User");
            await EnsureUser(reabetswe, "User123!", "User");

            // ── Categories ──────────────────────────────────────────
            if (!context.Categories.Any())
            {
                var food = new Category { CategoryId = Guid.NewGuid(), Name = "Food & Drink" };
                var restaurants = new Category { CategoryId = Guid.NewGuid(), Name = "Restaurants", ParentCategory = food };
                var cafes = new Category { CategoryId = Guid.NewGuid(), Name = "Cafés", ParentCategory = food };

                var services = new Category { CategoryId = Guid.NewGuid(), Name = "Professional Services" };
                var law = new Category { CategoryId = Guid.NewGuid(), Name = "Law Firms", ParentCategory = services };
                var accounting = new Category { CategoryId = Guid.NewGuid(), Name = "Accounting", ParentCategory = services };

                var retail = new Category { CategoryId = Guid.NewGuid(), Name = "Retail" };
                var clothing = new Category { CategoryId = Guid.NewGuid(), Name = "Clothing", ParentCategory = retail };
                var electronics = new Category { CategoryId = Guid.NewGuid(), Name = "Electronics", ParentCategory = retail };

                context.Categories.AddRange(food, restaurants, cafes, services, law, accounting, retail, clothing, electronics);
                await context.SaveChangesAsync();
            }

            // ── Businesses ─────────────────────────────────────────
            if (!context.Businesses.Any())
            {
                var thaboUser = await userManager.FindByEmailAsync("thabo@forum.co.za");
                var nalediUser = await userManager.FindByEmailAsync("naledi@forum.co.za");

                var restaurantsCat = context.Categories.First(c => c.Name == "Restaurants");
                var lawCat = context.Categories.First(c => c.Name == "Law Firms");
                var electronicsCat = context.Categories.First(c => c.Name == "Electronics");
                var accountingCat = context.Categories.First(c => c.Name == "Accounting");
                var cafesCat = context.Categories.First(c => c.Name == "Cafés");
                var clothingCat = context.Categories.First(c => c.Name == "Clothing");

                var businesses = new[]
                {
                    new Business { BusinessId = Guid.NewGuid(), Name = "Bloemfontein Bistro", CategoryId = restaurantsCat.CategoryId, OwnerId = thaboUser.Id, Address = "123 Main St, Bloemfontein", Phone = "051-123-4567", Email = "info@bistro.co.za", Description = "A cozy bistro serving Free State cuisine.", Region = "Free State", Status = "approved", CreatedAt = DateTime.UtcNow },
                    new Business { BusinessId = Guid.NewGuid(), Name = "Free State Legal Advisors", CategoryId = lawCat.CategoryId, OwnerId = nalediUser.Id, Address = "45 Court Rd, Bloemfontein", Phone = "051-987-6543", Email = "contact@fslegal.co.za", Description = "Legal services for businesses and individuals.", Region = "Free State", Status = "approved", CreatedAt = DateTime.UtcNow },
                    new Business { BusinessId = Guid.NewGuid(), Name = "TechZone Electronics", CategoryId = electronicsCat.CategoryId, OwnerId = thaboUser.Id, Address = "22 Tech Park, Johannesburg", Phone = "011-555-1234", Email = "sales@techzone.co.za", Description = "Electronics retailer with latest gadgets.", Region = "Gauteng", Status = "approved", CreatedAt = DateTime.UtcNow },
                    new Business { BusinessId = Guid.NewGuid(), Name = "Joburg Accounting Hub", CategoryId = accountingCat.CategoryId, OwnerId = nalediUser.Id, Address = "88 Finance St, Johannesburg", Phone = "011-222-3333", Email = "info@joburgaccounting.co.za", Description = "Accounting and tax services.", Region = "Gauteng", Status = "approved", CreatedAt = DateTime.UtcNow },
                    new Business { BusinessId = Guid.NewGuid(), Name = "Cape Town Coffee Co.", CategoryId = cafesCat.CategoryId, OwnerId = thaboUser.Id, Address = "12 Bree St, Cape Town", Phone = "021-444-5555", Email = "hello@ctcoffee.co.za", Description = "Artisan coffee roastery and café.", Region = "Western Cape", Status = "approved", CreatedAt = DateTime.UtcNow },
                    new Business { BusinessId = Guid.NewGuid(), Name = "Durban Fashion House", CategoryId = clothingCat.CategoryId, OwnerId = nalediUser.Id, Address = "77 Beach Rd, Durban", Phone = "031-777-8888", Email = "shop@durbanfashion.co.za", Description = "Trendy clothing boutique.", Region = "KwaZulu-Natal", Status = "approved", CreatedAt = DateTime.UtcNow }
                };

                context.Businesses.AddRange(businesses);
                await context.SaveChangesAsync();
            }

            // ── Listings ───────────────────────────────────────────
            if (!context.Listings.Any())
            {
                var businesses = context.Businesses.ToList();
                var listings = businesses.Select((b, i) => new Listing
                {
                    ListingId = Guid.NewGuid(),
                    BusinessId = b.BusinessId,
                    Tier = i % 3 == 0 ? "featured" : (i % 2 == 0 ? "premium" : "free"),
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(60),
                    PaymentStatus = i % 2 == 0 ? "paid" : "unpaid"
                });
                context.Listings.AddRange(listings);
                await context.SaveChangesAsync();
            }

            // ── Reviews ────────────────────────────────────────────
            if (!context.Reviews.Any())
            {
                var siphoUser = await userManager.FindByEmailAsync("sipho@forum.co.za");
                var reabetsweUser = await userManager.FindByEmailAsync("reabetswe@forum.co.za");
                var bistro = context.Businesses.First(b => b.Name == "Bloemfontein Bistro");
                var techzone = context.Businesses.First(b => b.Name == "TechZone Electronics");
                var coffee = context.Businesses.First(b => b.Name == "Cape Town Coffee Co.");

                var reviews = new[]
                {
                    new Review { ReviewId = Guid.NewGuid(), BusinessId = bistro.BusinessId, UserId = siphoUser.Id, Rating = 5, Comment = "Excellent food and cozy atmosphere!", CreatedAt = DateTime.UtcNow },
                    new Review { ReviewId = Guid.NewGuid(), BusinessId = techzone.BusinessId, UserId = reabetsweUser.Id, Rating = 4, Comment = "Great selection of gadgets.", CreatedAt = DateTime.UtcNow },
                    new Review { ReviewId = Guid.NewGuid(), BusinessId = coffee.BusinessId, UserId = siphoUser.Id, Rating = 5, Comment = "Best coffee in town!", CreatedAt = DateTime.UtcNow }
                };

                context.Reviews.AddRange(reviews);
                await context.SaveChangesAsync();
            }

            // ── Events ────────────────────────────────────────────
            if (!context.Events.Any())
            {
                var adminUser = await userManager.FindByEmailAsync("admin@forum.co.za");
                var nalediUser = await userManager.FindByEmailAsync("naledi@forum.co.za");

                var events = new[]
                {
                    new BusinessEvent { EventId = Guid.NewGuid(), Title = "Free State Business Forum Meetup", Description = "Networking event for local businesses.", Date = DateTime.UtcNow.AddDays(14), Location = "Bloemfontein Civic Centre", OrganizerId = adminUser.Id },
                    new BusinessEvent { EventId = Guid.NewGuid(), Title = "Cape Town Networking Night", Description = "Meet entrepreneurs and startups.", Date = DateTime.UtcNow.AddDays(30), Location = "Cape Town Conference Hall", OrganizerId = nalediUser.Id }
                };

                context.Events.AddRange(events);
                await context.SaveChangesAsync();
            }

            // ── AuditLogs ─────────────────────────────────────────
            if (!context.AuditLogs.Any())
            {
                var adminUser = await userManager.FindByEmailAsync("admin@forum.co.za");
                var logs = new[]
                {
                    new AuditLog { LogId = Guid.NewGuid(), UserId = adminUser.Id, Action = "Seeded initial roles and users", Timestamp = DateTime.UtcNow },
                    new AuditLog { LogId = Guid.NewGuid(), UserId = adminUser.Id, Action = "Seeded businesses and listings", Timestamp = DateTime.UtcNow }
                };

                context.AuditLogs.AddRange(logs);
                await context.SaveChangesAsync();
            }

            // ── UserFavorites ─────────────────────────────────────
            if (!context.UserFavorites.Any())
            {
                var siphoUser = await userManager.FindByEmailAsync("sipho@forum.co.za");
                var reabetsweUser = await userManager.FindByEmailAsync("reabetswe@forum.co.za");
                var bistro = context.Businesses.First(b => b.Name == "Bloemfontein Bistro");
                var techzone = context.Businesses.First(b => b.Name == "TechZone Electronics");

                var favorites = new[]
                {
                    new UserFavorite { UserId = siphoUser.Id, BusinessId = bistro.BusinessId, SavedAt = DateTime.UtcNow },
                    new UserFavorite { UserId = reabetsweUser.Id, BusinessId = techzone.BusinessId, SavedAt = DateTime.UtcNow }
                };

                context.UserFavorites.AddRange(favorites);
                await context.SaveChangesAsync();
            }

        }
    }
}
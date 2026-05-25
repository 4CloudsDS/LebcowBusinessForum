using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Data
{
    /// <summary>
    /// Production seed: creates one admin user and standard business categories.
    /// Safe to run on every startup — all operations are idempotent.
    /// Does NOT create sample businesses or dummy data.
    /// </summary>
    public static class SeedDataProduction
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            // ── Roles ───────────────────────────────────────────────────────────
            string[] roles = { "Admin", "BusinessOwner", "User" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new ApplicationRole { Name = roleName, NormalizedName = roleName.ToUpper() });
            }

            // ── Admin user ──────────────────────────────────────────────────────
            const string adminEmail = "marumanemogoswane@gmail.com";
            const string adminPassword = "Marumane@12345";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName    = adminEmail,
                    Email       = adminEmail,
                    FullName    = "Reabetswe",
                    CreatedAt   = DateTime.UtcNow,
                    IsActive    = true,
                    EmailConfirmed = true,
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Categories ──────────────────────────────────────────────────────
            // Only seed if the table is empty — preserves any categories added later via the UI.
            if (await context.Categories.AnyAsync())
                return;

            // Helper: create a parent category
            Category Parent(string name) => new() { CategoryId = Guid.NewGuid(), Name = name };

            // Helper: create a child category
            Category Child(string name, Category parent) => new()
            {
                CategoryId = Guid.NewGuid(),
                Name = name,
                ParentCategory = parent,
            };

            // ── Trades & Construction ───────────────────────────────────────────
            var trades = Parent("Trades & Construction");
            var tradesChildren = new[]
            {
                Child("Plumbing",               trades),
                Child("Electrical",             trades),
                Child("Building & Construction",trades),
                Child("Painting & Decorating",  trades),
                Child("Carpentry & Joinery",    trades),
                Child("Roofing",                trades),
                Child("Tiling & Flooring",      trades),
                Child("Landscaping & Gardening",trades),
                Child("Welding & Fabrication",  trades),
                Child("Air Conditioning & HVAC",trades),
                Child("Solar & Renewable Energy",trades),
            };

            // ── Professional Services ───────────────────────────────────────────
            var professional = Parent("Professional Services");
            var professionalChildren = new[]
            {
                Child("Legal Services",             professional),
                Child("Accounting & Finance",       professional),
                Child("Business Consulting",        professional),
                Child("Marketing & Advertising",    professional),
                Child("IT & Technology",            professional),
                Child("Human Resources",            professional),
                Child("Insurance",                  professional),
                Child("Real Estate",                professional),
            };

            // ── Food & Hospitality ──────────────────────────────────────────────
            var food = Parent("Food & Hospitality");
            var foodChildren = new[]
            {
                Child("Restaurants",            food),
                Child("Cafés & Coffee Shops",   food),
                Child("Catering",               food),
                Child("Takeaways & Fast Food",  food),
                Child("Bakeries",               food),
                Child("Food Delivery",          food),
            };

            // ── Retail ──────────────────────────────────────────────────────────
            var retail = Parent("Retail");
            var retailChildren = new[]
            {
                Child("Clothing & Fashion",     retail),
                Child("Electronics",            retail),
                Child("Home & Garden",          retail),
                Child("Health & Beauty",        retail),
                Child("Sports & Outdoors",      retail),
                Child("Books & Stationery",     retail),
                Child("General Goods",          retail),
            };

            // ── Health & Wellness ───────────────────────────────────────────────
            var health = Parent("Health & Wellness");
            var healthChildren = new[]
            {
                Child("Medical & Healthcare",   health),
                Child("Dental",                 health),
                Child("Pharmacy",               health),
                Child("Fitness & Gym",          health),
                Child("Spa & Beauty Salon",     health),
                Child("Optometry",              health),
                Child("Mental Health",          health),
            };

            // ── Education & Training ────────────────────────────────────────────
            var education = Parent("Education & Training");
            var educationChildren = new[]
            {
                Child("Schools & Tutoring",     education),
                Child("Vocational Training",    education),
                Child("Online Courses",         education),
                Child("Early Childhood",        education),
            };

            // ── Automotive ──────────────────────────────────────────────────────
            var automotive = Parent("Automotive");
            var automotiveChildren = new[]
            {
                Child("Car Sales & Dealerships",automotive),
                Child("Auto Repairs & Service", automotive),
                Child("Panel Beating",          automotive),
                Child("Car Wash & Valet",        automotive),
                Child("Tyres & Fitment",        automotive),
                Child("Auto Parts",             automotive),
            };

            // ── Transport & Logistics ───────────────────────────────────────────
            var transport = Parent("Transport & Logistics");
            var transportChildren = new[]
            {
                Child("Courier & Delivery",     transport),
                Child("Freight & Haulage",      transport),
                Child("Moving & Relocation",    transport),
                Child("Taxi & Shuttle",         transport),
            };

            // ── Entertainment & Events ──────────────────────────────────────────
            var entertainment = Parent("Entertainment & Events");
            var entertainmentChildren = new[]
            {
                Child("Events & Functions",         entertainment),
                Child("Photography & Videography",  entertainment),
                Child("Music & Entertainment",      entertainment),
                Child("Venue Hire",                 entertainment),
            };

            // ── Other ───────────────────────────────────────────────────────────
            var other = Parent("Other");

            // ── Persist ─────────────────────────────────────────────────────────
            var parents = new[] { trades, professional, food, retail, health, education, automotive, transport, entertainment, other };
            context.Categories.AddRange(parents);

            context.Categories.AddRange(tradesChildren);
            context.Categories.AddRange(professionalChildren);
            context.Categories.AddRange(foodChildren);
            context.Categories.AddRange(retailChildren);
            context.Categories.AddRange(healthChildren);
            context.Categories.AddRange(educationChildren);
            context.Categories.AddRange(automotiveChildren);
            context.Categories.AddRange(transportChildren);
            context.Categories.AddRange(entertainmentChildren);

            await context.SaveChangesAsync();
        }
    }
}

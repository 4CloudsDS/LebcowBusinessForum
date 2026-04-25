-- =============================================================================
-- LebcowBusinessForum — Data Seeding Script (Admin & Multi-Category Businesses)
-- =============================================================================
-- This script seeds the database with the admin user, relevant categories,
-- and businesses for each category, including an IT business for the admin.
-- =============================================================================

SET NOCOUNT ON;

-- ── 1. Admin User ───────────────────────────────────────────────────────────
-- Reabetswe Mogoswane (rmtjonko@gmail.com)

DECLARE @adminUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @adminRoleId UNIQUEIDENTIFIER;

-- Get Admin Role ID (Created in schema.sql)
SELECT @adminRoleId = Id FROM dbo.Roles WHERE NormalizedName = 'ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'rmtjonko@gmail.com')
BEGIN
    INSERT INTO dbo.Users (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, 
        PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
        LockoutEnd, LockoutEnabled, AccessFailedCount, 
        FullName, CreatedAt, IsActive
    )
    VALUES (
        @adminUserId, 
        'rmtjonko@gmail.com', 'RMTJONKO@GMAIL.COM', 
        'rmtjonko@gmail.com', 'RMTJONKO@GMAIL.COM', 
        1, 'AQAAAAEAACcQAAAAEPvX...', NEWID(), NEWID(), 
        '078 494 6161', 1, 0, 
        NULL, 1, 0, 
        'Reabetswe Mogoswane', GETUTCDATE(), 1
    );

    -- Link Admin to Role
    INSERT INTO dbo.UserRoles (UserId, RoleId)
    VALUES (@adminUserId, @adminRoleId);
END
ELSE
BEGIN
    SELECT @adminUserId = Id FROM dbo.Users WHERE Email = 'rmtjonko@gmail.com';
END;

-- ── 2. Ensure Categories Exist ───────────────────────────────────────────────
-- Top-level categories defined in schema.sql:
-- Retail & Shopping, Food & Restaurants, Professional Services, Health & Wellness,
-- Education & Training, Automotive, Construction & Trades, Technology & IT,
-- Events & Entertainment, Agriculture & Farming

-- ── 3. Seed Businesses for each Category ────────────────────────────────────

-- Helper to get Category IDs
DECLARE @catRetail UNIQUEIDENTIFIER, @catFood UNIQUEIDENTIFIER, @catProf UNIQUEIDENTIFIER,
        @catHealth UNIQUEIDENTIFIER, @catEdu UNIQUEIDENTIFIER, @catAuto UNIQUEIDENTIFIER,
        @catConst UNIQUEIDENTIFIER, @catIT UNIQUEIDENTIFIER, @catEvents UNIQUEIDENTIFIER,
        @catAgri UNIQUEIDENTIFIER;

SELECT @catRetail = CategoryId FROM dbo.Categories WHERE Name = 'Retail & Shopping';
SELECT @catFood = CategoryId FROM dbo.Categories WHERE Name = 'Food & Restaurants';
SELECT @catProf = CategoryId FROM dbo.Categories WHERE Name = 'Professional Services';
SELECT @catHealth = CategoryId FROM dbo.Categories WHERE Name = 'Health & Wellness';
SELECT @catEdu = CategoryId FROM dbo.Categories WHERE Name = 'Education & Training';
SELECT @catAuto = CategoryId FROM dbo.Categories WHERE Name = 'Automotive';
SELECT @catConst = CategoryId FROM dbo.Categories WHERE Name = 'Construction & Trades';
SELECT @catIT = CategoryId FROM dbo.Categories WHERE Name = 'Technology & IT';
SELECT @catEvents = CategoryId FROM dbo.Categories WHERE Name = 'Events & Entertainment';
SELECT @catAgri = CategoryId FROM dbo.Categories WHERE Name = 'Agriculture & Farming';

-- IT Business for Admin
IF NOT EXISTS (SELECT 1 FROM dbo.Businesses WHERE Name = 'RM Tech Solutions' OR OwnerId = @adminUserId)
BEGIN
    INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, OwnerId, CreatedAt)
    VALUES (
        NEWID(), 'RM Tech Solutions', @catIT, 
        '123 Digital Drive, Sandton, 2196', '078 494 6161', 
        'info@rmtech.io', 'https://marumanemogoswane-c9bfemhhggg3brdh.southafricanorth-01.azurewebsites.net/',
        'Premium IT consultancy and software development specializing in AI-driven automation and cloud architecture.',
        'active', 'Gauteng', @adminUserId, GETUTCDATE()
    );
END;

-- Retail
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'Lebowakgomo Mall Grocers', @catRetail, 'Zone F, Lebowakgomo, 0737', '015 633 1000', 'contact@lebcow-grocers.co.za', NULL, 'Large retail store providing essential goods to the local community.', 'active', 'Limpopo', GETUTCDATE());

-- Food
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'Village Kitchen', @catFood, 'Plot 12, Ha-Makhuvha, 0950', '015 962 1234', 'orders@villagekitchen.com', 'https://villagekitchen.com', 'Authentic Limpopo cuisine served daily.', 'active', 'Limpopo', GETUTCDATE());

-- Professional Services
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'Lekgotla Law Firm', @catProf, '45 West St, Cape Town, 8000', '021 424 5566', 'service@lekgotlalaw.co.za', 'https://lekgotlalaw.co.za', 'Specializing in commercial and customary law.', 'active', 'Western Cape', GETUTCDATE());

-- Health
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'Limpopo Wellness Center', @catHealth, 'Shop 5, Polokwane Square, 0700', '015 291 3000', 'info@limwellness.co.za', NULL, 'Holistic health services including physiotherapy and nutrition.', 'active', 'Limpopo', GETUTCDATE());

-- Education
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'TechSkills Academy', @catEdu, '88 Govan Mbeki Ave, Gqeberha, 6001', '041 585 9900', 'enroll@techskills.ac.za', 'https://techskills.ac.za', 'Empowering youth with 4IR vocational skills.', 'active', 'Eastern Cape', GETUTCDATE());

-- Automotive
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'Polokwane Auto Masters', @catAuto, 'Industrial Park, Polokwane, 0699', '015 297 8888', 'service@plkautomasters.co.za', NULL, 'Full-service automotive repair and parts center.', 'active', 'Limpopo', GETUTCDATE());

-- Construction
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'IronOak Construction', @catConst, '52 Main Rd, Richards Bay, 3900', '035 789 4455', 'build@ironoaka.com', 'https://ironoak.com', 'Civil engineering and residential development experts.', 'active', 'KwaZulu-Natal', GETUTCDATE());

-- Events
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'Safari Elegance Events', @catEvents, 'Hoedspruit Wildlife Estate, 1380', '015 793 0000', 'events@safarielegance.co.za', NULL, 'Curated event planning in the heart of the bushveld.', 'active', 'Mpumalanga', GETUTCDATE());

-- Agriculture
INSERT INTO dbo.Businesses (BusinessId, Name, CategoryId, Address, Phone, Email, Website, Description, Status, Region, CreatedAt)
VALUES (NEWID(), 'Sekhukhune Citrus Farms', @catAgri, 'Tubatse District, 1150', '013 231 7700', 'export@sekhukhune.farm', 'https://sekhukhune.farm', 'Supplier of export-quality citrus to global markets.', 'active', 'Limpopo', GETUTCDATE());

-- ── 4. Listings for Businesses ──────────────────────────────────────────────

INSERT INTO dbo.Listings (ListingId, BusinessId, Tier, StartDate, EndDate, PaymentStatus)
SELECT NEWID(), BusinessId, 'premium', GETUTCDATE(), DATEADD(year, 1, GETUTCDATE()), 'paid'
FROM dbo.Businesses WHERE Name = 'RM Tech Solutions';

INSERT INTO dbo.Listings (ListingId, BusinessId, Tier, StartDate, EndDate, PaymentStatus)
SELECT NEWID(), BusinessId, 'free', GETUTCDATE(), DATEADD(year, 1, GETUTCDATE()), 'paid'
FROM dbo.Businesses WHERE Name != 'RM Tech Solutions';

PRINT 'Data seeding complete.';
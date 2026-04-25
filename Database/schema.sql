-- =============================================================================
-- LebcowBusinessForum — Schema Setup Script
-- Provider : SQL Server (Azure SQL)
-- Stage    : scaffold_database (Stage 3)
-- =============================================================================
-- Run AFTER applying EF Core migrations (dotnet ef database update).
-- This script applies post-migration configuration:
--   1. Full-text search catalog and index
--   2. Seed data for roles and top-level categories
--   3. Region partitioning alignment comments
-- =============================================================================

-- ── Full-text search ──────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT * FROM sys.fulltext_catalogs WHERE name = N'LebcowFTSCatalog'
)
BEGIN
    CREATE FULLTEXT CATALOG LebcowFTSCatalog AS DEFAULT;
END;

IF NOT EXISTS (
    SELECT * FROM sys.fulltext_indexes fi
    JOIN sys.tables t ON fi.object_id = t.object_id
    WHERE t.name = N'Businesses'
)
BEGIN
    CREATE FULLTEXT INDEX ON dbo.Businesses
    (
        [Name]        LANGUAGE 1033,
        [Description] LANGUAGE 1033
    )
    KEY INDEX PK_Businesses
    ON LebcowFTSCatalog
    WITH STOPLIST = SYSTEM, CHANGE_TRACKING AUTO;
END;

-- ── Seed: ASP.NET Core Identity roles ─────────────────────────────────────────

SET NOCOUNT ON;

DECLARE @adminId   UNIQUEIDENTIFIER = NEWID();
DECLARE @ownerId   UNIQUEIDENTIFIER = NEWID();
DECLARE @userId    UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT @adminId, 'Admin', 'ADMIN', NEWID()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = 'ADMIN');

INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT @ownerId, 'BusinessOwner', 'BUSINESSOWNER', NEWID()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = 'BUSINESSOWNER');

INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT @userId, 'User', 'USER', NEWID()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = 'USER');

-- ── Seed: Top-level categories ────────────────────────────────────────────────

INSERT INTO dbo.Categories (CategoryId, Name, ParentCategoryId)
SELECT NEWID(), c.Name, NULL
FROM (VALUES
    ('Retail & Shopping'),
    ('Food & Restaurants'),
    ('Professional Services'),
    ('Health & Wellness'),
    ('Education & Training'),
    ('Automotive'),
    ('Construction & Trades'),
    ('Technology & IT'),
    ('Events & Entertainment'),
    ('Agriculture & Farming')
) AS c(Name)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Categories existing WHERE existing.Name = c.Name AND existing.ParentCategoryId IS NULL
);

-- ── Region reference values (for Business.Region column) ─────────────────────
-- Supported regions aligned to DB_requirements scalability (South African provinces):
--   Free State, Gauteng, Western Cape, Eastern Cape, KwaZulu-Natal,
--   Limpopo, Mpumalanga, North West, Northern Cape
-- No hard constraint at DB level — enforced by application layer.

-- ── Notes ─────────────────────────────────────────────────────────────────────
-- Geo-index: Address column is free-text. Google Maps geocoding is integrated
-- at application layer (FeatureEngineer Stage 7). A computed geography column
-- can be added post-Stage 7 when coordinates are available.
--
-- Partitioning by region: Future partition function on Region column.
-- Azure SQL Hyperscale recommended for national expansion.

PRINT 'LebcowBusinessForum schema setup complete.';

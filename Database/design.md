# LebcowBusinessForum — Database Design

**Stage:** 3 / 10 — `scaffold_database`  
**Persona:** DatabaseEngineer  
**Provider:** SQL Server (Azure SQL)  
**ORM:** Entity Framework Core 8.0

---

## Entity Relationship Diagram

```
Users (AspNetIdentity)
  │
  ├─owns──── Businesses ──has── Categories (self-ref tree)
  │             │
  │             ├──has──── Listings
  │             ├──has──── Reviews ←──written-by── Users
  │             └──in────  UserFavorites ←──saved-by── Users
  │
  ├─organises─ Events
  └─recorded-in AuditLogs
```

---

## Tables

### Users (ASP.NET Core Identity — `AspNetUsers` renamed to `Users`)
| Column | Type | Notes |
|--------|------|-------|
| Id | `uniqueidentifier` PK | GUID |
| FullName | `nvarchar(200)` | Required |
| Email | `nvarchar(256)` | Unique index |
| PasswordHash | `nvarchar(max)` | Identity-managed |
| CreatedAt | `datetime2` | Default `GETUTCDATE()` |
| IsActive | `bit` | Default `1` |
| + Identity columns | | LockoutEnd, TwoFactor, etc. |

### Roles (`Roles`)
Standard Identity roles table. Seeded: `Admin`, `BusinessOwner`, `User`.

### Categories
| Column | Type | Notes |
|--------|------|-------|
| CategoryId | `uniqueidentifier` PK | |
| Name | `nvarchar(100)` | Unique |
| ParentCategoryId | `uniqueidentifier` FK → Categories | Nullable, Restrict |

### Businesses
| Column | Type | Notes |
|--------|------|-------|
| BusinessId | `uniqueidentifier` PK | |
| Name | `nvarchar(200)` | Full-text indexed |
| CategoryId | `uniqueidentifier` FK → Categories | Restrict |
| OwnerId | `uniqueidentifier` FK → Users | Nullable, SetNull |
| Address | `nvarchar(500)` | |
| Phone | `nvarchar(30)` | |
| Email | `nvarchar(254)` | |
| Website | `nvarchar(500)` | Nullable |
| Description | `nvarchar(4000)` | Full-text indexed |
| LogoUrl | `nvarchar(500)` | Nullable |
| Status | `nvarchar(20)` | Default `pending` |
| Region | `nvarchar(100)` | SA province |
| CreatedAt | `datetime2` | Default `GETUTCDATE()` |

**Indexes:** CategoryId, OwnerId, Status, Region, FullText(Name+Description)

### Listings
| Column | Type | Notes |
|--------|------|-------|
| ListingId | `uniqueidentifier` PK | |
| BusinessId | `uniqueidentifier` FK → Businesses | Cascade |
| Tier | `nvarchar(20)` | `free`/`premium`/`featured` |
| StartDate | `datetime2` | |
| EndDate | `datetime2` | Indexed |
| PaymentStatus | `nvarchar(30)` | `unpaid`/`paid`/`refunded` |

### Reviews
| Column | Type | Notes |
|--------|------|-------|
| ReviewId | `uniqueidentifier` PK | |
| BusinessId | `uniqueidentifier` FK → Businesses | Cascade |
| UserId | `uniqueidentifier` FK → Users | Restrict |
| Rating | `int` | 1–5 (enforced in app layer) |
| Comment | `nvarchar(2000)` | Nullable |
| CreatedAt | `datetime2` | Default `GETUTCDATE()` |

**Unique constraint:** `(BusinessId, UserId)` — one review per user per business.

### Events
| Column | Type | Notes |
|--------|------|-------|
| EventId | `uniqueidentifier` PK | |
| Title | `nvarchar(200)` | |
| Description | `nvarchar(4000)` | Nullable |
| Date | `datetime2` | Indexed |
| Location | `nvarchar(500)` | Nullable |
| OrganizerId | `uniqueidentifier` FK → Users | Nullable, SetNull |

### AuditLogs
| Column | Type | Notes |
|--------|------|-------|
| LogId | `uniqueidentifier` PK | |
| UserId | `uniqueidentifier` FK → Users | Nullable, SetNull |
| Action | `nvarchar(500)` | |
| Timestamp | `datetime2` | Default `GETUTCDATE()`, Indexed |

### UserFavorites (join)
| Column | Type | Notes |
|--------|------|-------|
| UserId | `uniqueidentifier` FK → Users | Composite PK, Cascade |
| BusinessId | `uniqueidentifier` FK → Businesses | Composite PK, Cascade |
| SavedAt | `datetime2` | Default `GETUTCDATE()` |

---

## Full-Text Search

Applied via `schema.sql` post-migration:
- Catalog: `LebcowFTSCatalog`
- Index on `Businesses(Name, Description)`, language 1033 (English)
- Change tracking: AUTO

---

## Scalability Notes

- **Region partitioning:** `Business.Region` column supports future SQL Server partition function by South African province.
- **Geo-index:** Address is free-text; Google Maps geocoding (Stage 7) will populate a computed geography column for spatial queries.
- **Azure SQL Hyperscale** recommended for national/global expansion.

---

## Migrations

| Migration | Date | Description |
|-----------|------|-------------|
| `20260424000000_InitialCreate` | 2026-04-24 | All core tables, FK constraints, indexes, Identity tables renamed |

Apply: `dotnet ef database update` from `LebcowBusinessForum.Web/` directory.

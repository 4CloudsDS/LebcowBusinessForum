# Database Validation Report
**Project:** LebcowBusinessForum  
**Stage:** 4 / 10 — `test_database`  
**Persona:** TestAnalyst  
**Date:** 2026-04-24  
**Result:** PASS

---

## 1. Summary

The Stage 3 database artifacts (`ApplicationDbContext.OnModelCreating`, migration file `20260424000000_InitialCreate.cs`, `schema.sql`, `design.md`) have been reviewed against the DB requirements, functional requirements, and EF Core 8 best practices. All 7 core tables are correctly defined with appropriate FK constraints, indexes, and delete behaviors. No blocking defects found.

---

## 2. Schema Checklist

### 2.1 Tables vs DB Requirements

| Required Table | Mapped Entity | EF Config | Status |
|----------------|--------------|-----------|--------|
| `users` | `ApplicationUser` | `Users` table, FullName max 200, Email unique | PASS |
| `businesses` | `Business` | Name/Address/Phone/Email/Description/Status/Region, FK Category (Restrict), FK User (SetNull) | PASS |
| `categories` | `Category` | Name unique, self-ref ParentCategoryId (Restrict) | PASS |
| `listings` | `Listing` | Tier default "free", PaymentStatus, FK Businesses (Cascade) | PASS |
| `reviews` | `Review` | Rating int, unique (BusinessId+UserId), FK Businesses (Cascade), FK Users (Restrict) | PASS |
| `events` | `BusinessEvent` | Title max 200, Date indexed, FK OrganizerId (SetNull) | PASS |
| `audit_logs` | `AuditLog` | Action max 500, Timestamp indexed, FK UserId (SetNull) | PASS |

### 2.2 Relationships vs Requirements

| Requirement | Implementation | Status |
|-------------|---------------|--------|
| One-to-many: Categories → Businesses | `Business.CategoryId` FK with `Restrict` | PASS |
| One-to-many: Businesses → Reviews | `Review.BusinessId` FK with `Cascade` | PASS |
| Many-to-many: Users ↔ Businesses (favorites) | `UserFavorites` join table, composite PK `(UserId, BusinessId)`, both Cascade | PASS |
| Owner relationship (application-level) | `Business.OwnerId` FK → Users with `SetNull` | PASS |

### 2.3 Identity Integration

| Check | Status | Notes |
|-------|--------|-------|
| `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` | PASS | Correct generic parameters |
| `base.OnModelCreating(builder)` called | PASS | Required first call |
| Identity tables renamed from AspNet* to short names | PASS | Users, Roles, UserRoles, UserClaims, UserLogins, RoleClaims, UserTokens |
| `ApplicationUser` extends `IdentityUser<Guid>` | PASS | Guid primary key throughout |

### 2.4 Indexes

| Index | Table | Type | Status |
|-------|-------|------|--------|
| `IX_Categories_Name` | Categories | Unique | PASS |
| `IX_Categories_ParentCategoryId` | Categories | Non-unique | PASS |
| `IX_Businesses_CategoryId` | Businesses | Non-unique | PASS |
| `IX_Businesses_OwnerId` | Businesses | Non-unique | PASS |
| `IX_Businesses_Status` | Businesses | Non-unique | PASS |
| `IX_Businesses_Region` | Businesses | Non-unique | PASS |
| `IX_Businesses_FullText` (Name+Description) | Businesses | Full-text (schema.sql) | PASS |
| `IX_Listings_BusinessId` | Listings | Non-unique | PASS |
| `IX_Listings_Tier` | Listings | Non-unique | PASS |
| `IX_Listings_EndDate` | Listings | Non-unique | PASS |
| `IX_Reviews_BusinessId` | Reviews | Non-unique | PASS |
| `IX_Reviews_BusinessId_UserId` | Reviews | Unique composite | PASS |
| `IX_Events_Date` | Events | Non-unique | PASS |
| `IX_Events_OrganizerId` | Events | Non-unique | PASS |
| `IX_AuditLogs_Timestamp` | AuditLogs | Non-unique | PASS |
| `IX_UserFavorites_UserId` | UserFavorites | Non-unique | PASS |
| `IX_UserFavorites_BusinessId` | UserFavorites | Non-unique | PASS |

### 2.5 Delete Behavior Correctness

| Relationship | Behavior | Reasoning | Status |
|-------------|----------|-----------|--------|
| Category → Businesses | Restrict | Cannot delete a category with businesses | PASS |
| Category self-ref | Restrict | Prevent accidental parent deletion | PASS |
| Business → Listings | Cascade | Listings are owned by business | PASS |
| Business → Reviews | Cascade | Reviews are owned by business | PASS |
| User → Reviews (writer) | Restrict | Preserve data integrity, soft-delete preferred | PASS |
| User → AuditLogs | SetNull | Anonymise logs on user deletion | PASS |
| User → Events (organiser) | SetNull | Event survives user deletion | PASS |
| User → Business (owner) | SetNull | Business survives owner deletion | PASS |
| User → UserFavorites | Cascade | Remove user's saved items | PASS |
| Business → UserFavorites | Cascade | Remove saves when business deleted | PASS |

### 2.6 schema.sql Post-Migration Script

| Check | Status | Notes |
|-------|--------|-------|
| Full-text catalog created idempotently | PASS | `IF NOT EXISTS` guard |
| Full-text index on `Businesses(Name, Description)` | PASS | Language 1033, CHANGE_TRACKING AUTO |
| Role seeds idempotent | PASS | `WHERE NOT EXISTS` guards |
| Category seeds idempotent | PASS | 10 top-level categories including Agriculture |
| Region values documented | PASS | SA provinces listed in comments |

---

## 3. Requirements Traceability

| DB Requirement | Coverage |
|----------------|---------|
| Full-text search on business name and description | PASS — FTS index defined in schema.sql |
| Geo-index on addresses | ADVISORY — deferred to Stage 7 (Google Maps integration needed first) |
| One-to-many: Categories → Businesses | PASS |
| One-to-many: Businesses → Reviews | PASS |
| Many-to-many: Users ↔ Businesses (favorites) | PASS |
| Partitioning by region | ADVISORY — Region column present; SQL Server partition function deferred to deployment |

---

## 4. Defects

No blocking defects.

| Severity | Item |
|----------|------|
| Advisory | Geo-index deferred to Stage 7 — Google Maps coordinates not yet available |
| Advisory | Rating range (1–5) enforced in application layer only; DB CHECK constraint deferred |
| Advisory | `dotnet ef database update` not yet run — requires live connection string; Azure SQL provisioned at deployment stage |

---

## 5. Verdict

**PASS** — Database schema is correctly designed, all FK constraints and delete behaviors are appropriate, all 7 required tables are present with proper indexes and identity integration. Advisory items are correctly deferred to later stages.

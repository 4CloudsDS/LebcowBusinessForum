# Backend Services — LebcowBusinessForum

This folder serves as the stage output marker for Stage 5 (`implement_backend`).

The actual service layer was implemented in-place within the web project at:
`LebcowBusinessForum.Web/Services/`

## Files Created / Modified

### New Service Files
- `Services/IAuditService.cs` — Interface: `LogAsync(Guid userId, string action)`
- `Services/IBusinessService.cs` — Interface: CRUD, search, approval operations
- `Services/IReviewService.cs` — Interface: submit, list, duplicate-check
- `Services/AuditService.cs` — Writes `AuditLog` records to DB
- `Services/BusinessService.cs` — Full business lifecycle service (search, create, approve, reject, owner query)
- `Services/ReviewService.cs` — Rating 1–5 validation, unique-review enforcement

### Modified Page Models
- `Pages/Account/Dashboard.cshtml.cs` — Uses `IBusinessService.GetByOwnerAsync(userId)` (OwnerId FK)
- `Pages/Browse.cshtml.cs` — Delegates to `IBusinessService.SearchAsync`
- `Pages/Business.cshtml.cs` — Uses `IReviewService`; adds POST review handler
- `Pages/BusinessOwner/Create.cshtml.cs` — Sets `OwnerId`, delegates to `IBusinessService.CreateAsync`
- `Pages/Index.cshtml.cs` — Uses `IBusinessService.GetFeaturedAsync`
- `Pages/Admin/Index.cshtml.cs` — Approve/reject handlers via `IBusinessService`

### Modified Bootstrap
- `Program.cs` — Scoped registration of `IAuditService`, `IBusinessService`, `IReviewService`

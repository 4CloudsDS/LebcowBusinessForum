# Features — LebcowBusinessForum Stage 7

Stage output marker for `implement_features`.

## Features Implemented

### Forum (`/Forum`)
- `Pages/Forum.cshtml` + `Forum.cshtml.cs` (ForumModel)
- `Models/ForumPost.cs` — PostId, AuthorId, Title, Body, CreatedAt
- `ApplicationDbContext` — `ForumPosts` DbSet + EF configuration (FK → Users SetNull, indexes on CreatedAt/AuthorId)

### Newsletter (`/Newsletter`)
- `Pages/Newsletter.cshtml` + `Newsletter.cshtml.cs` (NewsletterModel)
- `Models/NewsletterSubscription.cs` — SubscriptionId, Email, SubscribedAt, IsActive
- `ApplicationDbContext` — `NewsletterSubscriptions` DbSet + unique index on Email
- Idempotent subscribe (re-activates inactive subscriptions)

### UserFavorites Toggle
- `Pages/Account/Favourites.cshtml.cs` — `OnPostToggleAsync(Guid businessId)` adds/removes favourite
- POST to `/Account/Favourites?handler=Toggle&businessId={id}` — returns to referer or Dashboard

### Google Maps
- `appsettings.json` — `AppSettings:GoogleMapsApiKey` config key added
- `Business.cshtml.cs` — reads key via `IConfiguration`, sets `ViewData["GoogleMapsKey"]`
- `Business.cshtml` — conditional Maps embed iframe (with API key) or plain Maps link fallback

### Payment / Listing Upgrade
- `Pages/BusinessOwner/UpgradeListing.cshtml` + `UpgradeListing.cshtml.cs`
- Creates a `Listing` record with `PaymentStatus = "pending"`
- Payment comment indicates PayFast (ZA) / Stripe webhook integration point
- Tier validation server-side (premium | featured only)
- Owner-only access via `IBusinessService.IsOwnerAsync`

### Review Form on Business Detail
- `Business.cshtml` — review form with rating select + comment textarea
- `Business.cshtml.cs` — `OnPostReviewAsync` handler (replaces stub)
- Displays author name, duplicate review message, sign-in prompt for guests

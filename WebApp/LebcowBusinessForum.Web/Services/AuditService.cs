using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;

namespace LebcowBusinessForum.Web.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(Guid userId, string action)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            LogId = Guid.NewGuid(),
            UserId = userId,
            Action = action.Length > 500 ? action[..500] : action,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}

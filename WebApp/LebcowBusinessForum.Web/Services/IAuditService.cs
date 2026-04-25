using LebcowBusinessForum.Web.Models;

namespace LebcowBusinessForum.Web.Services;

public interface IAuditService
{
    Task LogAsync(Guid userId, string action);
}

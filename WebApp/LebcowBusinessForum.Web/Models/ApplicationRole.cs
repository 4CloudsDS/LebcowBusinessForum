using Microsoft.AspNetCore.Identity;

namespace LebcowBusinessForum.Web.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}

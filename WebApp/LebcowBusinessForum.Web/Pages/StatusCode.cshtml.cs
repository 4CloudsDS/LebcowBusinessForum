using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LebcowBusinessForum.Web.Pages;

public class StatusCodeModel : PageModel
{
    public int HttpStatusCode { get; private set; }

    public void OnGet(int code)
    {
        HttpStatusCode = code;
    }
}

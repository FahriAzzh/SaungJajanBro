using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SAUNGJAJAN.Pages.Auth
{
    public class LogoutTokoModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.Remove("id_toko");
            HttpContext.Session.Remove("NamaToko");
            HttpContext.Session.Remove("LoginSebagai");

            return RedirectToPage("/Index");
        }
    }
}
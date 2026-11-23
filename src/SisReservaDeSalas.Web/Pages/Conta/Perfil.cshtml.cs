using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SisReservaDeSalas.Web.Pages.Conta;

[Authorize]
public class PerfilModel : PageModel
{
    public void OnGet() { }
}

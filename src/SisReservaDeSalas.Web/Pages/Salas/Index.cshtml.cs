using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Salas;

[Authorize]
public class IndexModel(SalaService salas) : PageModel
{
    public List<Sala> Salas { get; private set; } = [];
    public async Task OnGetAsync() => Salas = await salas.ListarAtivasAsync();
}

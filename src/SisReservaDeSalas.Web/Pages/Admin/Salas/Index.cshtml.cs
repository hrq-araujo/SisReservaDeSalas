using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Admin.Salas;

[Authorize(Roles = Perfis.Administrador)]
public class IndexModel(SalaService salas) : PageModel
{
    public List<Sala> Salas { get; private set; } = [];
    public async Task OnGetAsync() => Salas = await salas.ListarTodasAsync();
}

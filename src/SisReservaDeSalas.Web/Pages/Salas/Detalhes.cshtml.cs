using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Salas;

[Authorize]
public class DetalhesModel(SalaService salas) : PageModel
{
    public Sala Sala { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sala = await salas.BuscarAtivaAsync(id);
        if (sala is null)
            return NotFound();

        Sala = sala;
        return Page();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Admin.Salas;

[Authorize(Roles = Perfis.Administrador)]
public class SituacaoModel(SalaService salas) : PageModel
{
    public Sala Sala { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sala = await salas.BuscarAsync(id);
        if (sala is null) return NotFound();
        Sala = sala;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, bool? ativa)
    {
        if (!ativa.HasValue) return BadRequest();
        if (!await salas.AlterarSituacaoAsync(id, ativa.Value)) return NotFound();
        TempData["Sucesso"] = ativa.Value ? "Sala reativada." : "Sala desativada. As reservas existentes foram mantidas.";
        return RedirectToPage("/Admin/Salas/Index");
    }
}

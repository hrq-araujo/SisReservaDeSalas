using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Reservas;

[Authorize]
public class CriarModel(ReservaService reservas, SalaService salas, UserManager<IdentityUser> usuarios) : PageModel
{
    [BindProperty]
    public CadastroReserva Cadastro { get; set; } = new();
    public Sala Sala { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int salaId, DateOnly? data, TimeOnly? horaInicio, TimeOnly? horaFim)
    {
        var sala = await salas.BuscarAtivaAsync(salaId);
        if (sala is null) return NotFound();
        Sala = sala;
        Cadastro = new() { SalaId = salaId, Data = data, HoraInicio = horaInicio, HoraFim = horaFim };
        ModelState.Clear();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sala = await salas.BuscarAtivaAsync(Cadastro.SalaId);
        if (sala is null) return NotFound();
        Sala = sala;
        if (!ModelState.IsValid) return Page();
        var resultado = await reservas.CriarAsync(Cadastro, usuarios.GetUserId(User)!);
        if (resultado.Erro is not null)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro);
            return Page();
        }
        return RedirectToPage("/Reservas/Confirmacao", new { id = resultado.Id });
    }
}

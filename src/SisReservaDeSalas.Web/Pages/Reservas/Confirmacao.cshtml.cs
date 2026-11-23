using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Reservas;

[Authorize]
public class ConfirmacaoModel(ReservaService reservas, UserManager<IdentityUser> usuarios) : PageModel
{
    public Reserva Reserva { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var reserva = await reservas.BuscarDoUsuarioAsync(id, usuarios.GetUserId(User)!);
        if (reserva is null) return NotFound();
        Reserva = reserva;
        return Page();
    }
}

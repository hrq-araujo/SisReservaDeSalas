using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Reservas;

[Authorize]
public class MinhasModel(ReservaService reservas, UserManager<IdentityUser> usuarios) : PageModel
{
    public List<Reserva> Reservas { get; private set; } = [];
    public DateTime Agora { get; } = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
        DateTimeOffset.UtcNow, "America/Sao_Paulo").DateTime;

    public async Task OnGetAsync() =>
        Reservas = await reservas.ListarDoUsuarioAsync(usuarios.GetUserId(User)!);
}

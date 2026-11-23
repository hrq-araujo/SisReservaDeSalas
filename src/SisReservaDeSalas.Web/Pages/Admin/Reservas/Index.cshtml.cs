using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Admin.Reservas;

[Authorize(Roles = Perfis.Administrador)]
public class IndexModel(ReservaService reservas) : PageModel
{
    public List<ReservaAdministrativa> Reservas { get; private set; } = [];
    public DateTime Agora { get; } = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
        DateTimeOffset.UtcNow, "America/Sao_Paulo").DateTime;

    public async Task OnGetAsync() => Reservas = await reservas.ListarAdministrativamenteAsync();
}

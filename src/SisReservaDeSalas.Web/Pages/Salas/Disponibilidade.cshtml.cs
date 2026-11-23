using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Salas;

[Authorize]
public class DisponibilidadeModel(ReservaService reservas) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public PeriodoReserva Periodo { get; set; } = new();
    public List<Sala> Salas { get; private set; } = [];
    public bool Consultou { get; private set; }

    public async Task OnGetAsync(bool consultar = false)
    {
        if (!consultar)
        {
            ModelState.Clear();
            return;
        }
        if (!ModelState.IsValid) return;
        Salas = await reservas.ConsultarDisponiveisAsync(Periodo);
        Consultou = true;
    }
}

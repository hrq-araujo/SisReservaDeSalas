using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Admin.Salas;

[Authorize(Roles = Perfis.Administrador)]
public class EditarModel(SalaService salas) : PageModel
{
    [BindProperty]
    public CadastroSala Cadastro { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sala = await salas.BuscarAsync(id);
        if (sala is null) return NotFound();
        Cadastro = new() { Identificacao = sala.Identificacao, Localizacao = sala.Localizacao,
            Capacidade = sala.Capacidade, Recursos = sala.Recursos };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (await salas.BuscarAsync(id) is null) return NotFound();
        if (!ModelState.IsValid) return Page();
        if (!await salas.EditarAsync(id, Cadastro)) return NotFound();
        TempData["Sucesso"] = "Dados da sala atualizados.";
        return RedirectToPage("/Admin/Salas/Index");
    }
}

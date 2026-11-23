using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Web.Pages.Salas;

[Authorize(Roles = Perfis.Administrador)]
public class CadastrarModel(SalaService salas) : PageModel
{
    [BindProperty]
    public CadastroSala Cadastro { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var id = await salas.CadastrarAsync(Cadastro);
        TempData["Sucesso"] = "Sala cadastrada com sucesso.";
        return RedirectToPage("/Salas/Detalhes", new { id });
    }
}

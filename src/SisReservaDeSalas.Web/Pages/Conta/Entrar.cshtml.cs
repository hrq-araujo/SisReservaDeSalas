using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SisReservaDeSalas.Web.Pages.Conta;

[AllowAnonymous]
public class EntrarModel(SignInManager<IdentityUser> signInManager) : PageModel
{
    [BindProperty]
    public DadosEntrada Entrada { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Conta/Perfil");

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
            return Page();

        var result = await signInManager.PasswordSignInAsync(
            Entrada.Email.Trim(), Entrada.Senha, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
            return Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl!)
                : RedirectToPage("/Conta/Perfil");

        ModelState.AddModelError(string.Empty,
            "Não foi possível entrar. Confira seus dados ou aguarde alguns minutos antes de tentar novamente.");
        return Page();
    }

    public class DadosEntrada
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;
    }
}

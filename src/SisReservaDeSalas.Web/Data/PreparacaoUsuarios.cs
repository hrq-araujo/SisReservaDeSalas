using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using SisReservaDeSalas.Web.Models;

namespace SisReservaDeSalas.Web.Data;

public static class PreparacaoUsuarios
{
    public static async Task ExecutarAsync(IServiceProvider services, IConfiguration configuration)
    {
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var contas = new[]
        {
            LerConta(configuration, "Administrador", Perfis.Administrador),
            LerConta(configuration, "Usuario", Perfis.Usuario)
        };

        if (string.Equals(contas[0].Email, contas[1].Email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Informe e-mails diferentes para os dois perfis.");

        foreach (var conta in contas)
        {
            if (!await roles.RoleExistsAsync(conta.Perfil))
                Conferir(await roles.CreateAsync(new IdentityRole(conta.Perfil)));

            var user = await users.FindByEmailAsync(conta.Email);
            if (user is null)
            {
                user = new IdentityUser { UserName = conta.Email, Email = conta.Email };
                Conferir(await users.CreateAsync(user, conta.Senha));
            }

            if (!await users.IsInRoleAsync(user, conta.Perfil))
                Conferir(await users.AddToRoleAsync(user, conta.Perfil));
        }
    }

    private static (string Email, string Senha, string Perfil) LerConta(
        IConfiguration configuration, string nome, string perfil)
    {
        var email = configuration[$"UsuariosIniciais:{nome}:Email"]?.Trim();
        var senha = configuration[$"UsuariosIniciais:{nome}:Senha"];
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email)
            || string.IsNullOrWhiteSpace(senha))
            throw new InvalidOperationException($"Configure e-mail e senha válidos em UsuariosIniciais:{nome}.");

        return (email, senha, perfil);
    }

    private static void Conferir(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Não foi possível preparar a conta: " +
                string.Join(", ", result.Errors.Select(e => e.Code)));
    }
}

using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisReservaDeSalas.Web.Data;
using SisReservaDeSalas.Web.Models;

namespace SisReservaDeSalas.Tests;

public class ConsultaAdministrativaTests
{
    [Fact]
    public Task AdministradorConsultaReservasDeTodosOsUsuarios() => VerificarConsulta(usarPostgres: false);

    [PostgresFact]
    public Task ConsultaAdministrativaFuncionaNoPostgreSql() => VerificarConsulta(usarPostgres: true);

    private static async Task VerificarConsulta(bool usarPostgres)
    {
        await using var app = new AplicacaoTeste(usarPostgres);
        using var client = app.CriarCliente();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
            var usuario = await db.Users.SingleAsync(u => u.Email == "usuario@teste.local");
            var admin = await db.Users.SingleAsync(u => u.Email == "admin@teste.local");
            db.Salas.AddRange(
                new Sala { Id = 1, Identificacao = "Sala ativa", Localizacao = "Bloco C", Capacidade = 30 },
                new Sala { Id = 2, Identificacao = "Sala inativa", Localizacao = "Bloco D", Capacidade = 20, Ativa = false });
            db.Reservas.AddRange(
                new Reserva { SalaId = 1, UsuarioId = usuario.Id, Inicio = new(2099, 1, 1, 9, 0, 0),
                    Fim = new(2099, 1, 1, 10, 0, 0), Finalidade = "Aula do usuário" },
                new Reserva { SalaId = 2, UsuarioId = admin.Id, Inicio = new(2099, 1, 2, 14, 0, 0),
                    Fim = new(2099, 1, 2, 15, 0, 0), Finalidade = "Reunião cancelada", Ativa = false });
            await db.SaveChangesAsync();
        }
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        var response = await client.GetAsync("/Admin/Reservas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        foreach (var texto in new[] { "Sala ativa", "Sala inativa", "usuario@teste.local", "admin@teste.local",
                     "01/01/2099", "02/01/2099", "09:00 às 10:00", "14:00 às 15:00",
                     "Aula do usuário", "Reunião cancelada", "Agendada", "Cancelada" })
            Assert.Contains(texto, html);
        Assert.True(html.IndexOf("Reunião cancelada", StringComparison.Ordinal)
            < html.IndexOf("Aula do usuário", StringComparison.Ordinal));
        Assert.DoesNotContain("/Reservas/Cancelar", html);
        Assert.DoesNotContain("PasswordHash", html);
    }

    [Fact]
    public async Task VisitanteEUsuarioComumNaoConsultamTodasAsReservas()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        var visitante = await client.GetAsync("/Admin/Reservas");
        Assert.Equal(HttpStatusCode.Redirect, visitante.StatusCode);
        Assert.Contains("/Conta/Entrar", visitante.Headers.Location!.ToString());
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        var usuario = await client.GetAsync("/Admin/Reservas");
        Assert.Equal(HttpStatusCode.Redirect, usuario.StatusCode);
        Assert.Contains("/Conta/AcessoNegado", usuario.Headers.Location!.ToString());
        Assert.DoesNotContain("Todas as reservas", await client.GetStringAsync("/Conta/Perfil"));
    }

    [Fact]
    public async Task ListaVaziaApresentaMensagem()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        var html = await client.GetStringAsync("/Admin/Reservas");
        Assert.Contains("Nenhuma reserva registrada.", html);
    }
}

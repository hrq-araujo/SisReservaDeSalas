using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisReservaDeSalas.Web.Data;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Tests;

public class CancelamentoTests
{
    [Fact]
    public async Task ListaMostraSomenteReservasPropriasComSituacao()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Reservas/Minhas"));
        Assert.Contains("Sala própria", html);
        Assert.Contains("Agendada", html);
        Assert.Contains("Cancelada", html);
        Assert.Contains("Encerrada", html);
        Assert.DoesNotContain("Sala de outra pessoa", html);
    }

    [Fact]
    public Task CancelamentoPreservaHistoricoELiberaHorario() => VerificarCancelamento(usarPostgres: false);

    [PostgresFact]
    public Task CancelamentoLiberaRestricaoDeHorarioNoPostgreSql() => VerificarCancelamento(usarPostgres: true);

    private static async Task VerificarCancelamento(bool usarPostgres)
    {
        await using var app = new AplicacaoTeste(usarPostgres);
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        var pagina = await client.GetStringAsync("/Reservas/Cancelar/1");
        using (var scope = app.Services.CreateScope())
            Assert.True((await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Reservas.FindAsync(1))!.Ativa);

        var response = await Cancelar(client, 1, pagina);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Reservas/Minhas", response.Headers.Location!.ToString());
        var lista = WebUtility.HtmlDecode(await client.GetStringAsync(response.Headers.Location));
        Assert.Contains("Reserva cancelada. O horário foi liberado.", lista);
        var detalhes = await client.GetStringAsync("/Reservas/Confirmacao/1");
        Assert.Contains("Reserva cancelada", detalhes);
        Assert.DoesNotContain(">Cancelar reserva</a>", detalhes);

        using var consulta = app.Services.CreateScope();
        var db = consulta.ServiceProvider.GetRequiredService<ReservaDbContext>();
        var reserva = await db.Reservas.AsNoTracking().SingleAsync(r => r.Id == 1);
        Assert.False(reserva.Ativa);
        Assert.Equal(4, await db.Reservas.CountAsync());
        Assert.Equal("Atividade original", reserva.Finalidade);
        Assert.Equal(new DateTime(2099, 1, 1, 9, 0, 0), reserva.Inicio);
        Assert.Equal(new DateTime(2099, 1, 1, 10, 0, 0), reserva.Fim);
        var service = consulta.ServiceProvider.GetRequiredService<ReservaService>();
        var cadastro = new CadastroReserva
        {
            SalaId = 1, Data = new(2099, 1, 1), HoraInicio = new(9, 0), HoraFim = new(10, 0), Finalidade = "Nova atividade"
        };
        Assert.Contains(await service.ConsultarDisponiveisAsync(cadastro), s => s.Id == 1);
        Assert.NotNull((await service.CriarAsync(cadastro, reserva.UsuarioId)).Id);

        // Repetir o cancelamento antigo não pode afetar a nova reserva.
        Assert.Equal(HttpStatusCode.Redirect, (await Cancelar(client, 1, pagina)).StatusCode);
        Assert.Single(await db.Reservas.AsNoTracking().Where(r => r.SalaId == 1 && r.Ativa && r.Inicio == reserva.Inicio).ToListAsync());
    }

    [Fact]
    public async Task OutraPessoaNaoCancelaReservaMesmoSendoAdministrador()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Reservas/Cancelar/1")).StatusCode);
        var pagina = await client.GetStringAsync("/Conta/Perfil");
        Assert.Equal(HttpStatusCode.NotFound, (await Cancelar(client, 1, pagina)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Cancelar(client, 999, pagina)).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Reservas.FindAsync(1))!.Ativa);
    }

    [Fact]
    public async Task CancelamentoSemTokenEhRecusado()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        var response = await client.PostAsync("/Reservas/Cancelar/1", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Reservas.FindAsync(1))!.Ativa);
    }

    [Fact]
    public async Task VisitantePrecisaEntrarEListaVaziaTemMensagem()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Reservas/Minhas")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Reservas/Cancelar/1")).StatusCode);
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        Assert.Contains("Você ainda não realizou reservas.", WebUtility.HtmlDecode(await client.GetStringAsync("/Reservas/Minhas")));
    }

    private static Task<HttpResponseMessage> Cancelar(HttpClient client, int id, string pagina) =>
        client.PostAsync($"/Reservas/Cancelar/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(pagina)
        }));

    private static async Task Preparar(AplicacaoTeste app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
        var usuario = await db.Users.SingleAsync(u => u.Email == "usuario@teste.local");
        var admin = await db.Users.SingleAsync(u => u.Email == "admin@teste.local");
        db.Salas.AddRange(
            new Sala { Id = 1, Identificacao = "Sala própria", Localizacao = "Bloco C", Capacidade = 30 },
            new Sala { Id = 2, Identificacao = "Sala de outra pessoa", Localizacao = "Bloco C", Capacidade = 30 });
        db.Reservas.AddRange(
            new Reserva { Id = 1, SalaId = 1, UsuarioId = usuario.Id, Inicio = new(2099, 1, 1, 9, 0, 0), Fim = new(2099, 1, 1, 10, 0, 0), Finalidade = "Atividade original" },
            new Reserva { Id = 2, SalaId = 1, UsuarioId = usuario.Id, Inicio = new(2099, 1, 2, 9, 0, 0), Fim = new(2099, 1, 2, 10, 0, 0), Finalidade = "Cancelada", Ativa = false },
            new Reserva { Id = 3, SalaId = 2, UsuarioId = admin.Id, Inicio = new(2099, 1, 1, 9, 0, 0), Fim = new(2099, 1, 1, 10, 0, 0), Finalidade = "Outra pessoa" },
            new Reserva { Id = 4, SalaId = 1, UsuarioId = usuario.Id, Inicio = new(2000, 1, 1, 9, 0, 0), Fim = new(2000, 1, 1, 10, 0, 0), Finalidade = "Atividade antiga" });
        await db.SaveChangesAsync();
        if (db.Database.IsNpgsql())
        {
            // IDs fixos do cenário de teste não avançam a sequência do PostgreSQL.
            await db.Database.ExecuteSqlRawAsync("""
                SELECT setval(pg_get_serial_sequence('"Reservas"', 'Id'), (SELECT MAX("Id") FROM "Reservas"));
                """);
        }
    }
}

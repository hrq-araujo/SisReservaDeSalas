using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SisReservaDeSalas.Web.Data;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Tests;

public class GerenciamentoSalasTests
{
    [Fact]
    public async Task AdministradorEditaDesativaEReativaSemApagarReservas()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        var pagina = await client.GetStringAsync("/Admin/Salas/Editar/1");
        var editar = await client.PostAsync("/Admin/Salas/Editar/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(pagina),
            ["Cadastro.Identificacao"] = "Sala editada", ["Cadastro.Localizacao"] = "Bloco D",
            ["Cadastro.Capacidade"] = "40", ["Cadastro.Recursos"] = "Projetor",
            ["Cadastro.Ativa"] = "false", ["Cadastro.Id"] = "999"
        }));
        Assert.Equal(HttpStatusCode.Redirect, editar.StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
            var sala = await db.Salas.SingleAsync();
            Assert.True(sala.Ativa);
            Assert.Equal("Bloco D", sala.Localizacao);
            Assert.Equal(40, sala.Capacidade);
            Assert.Equal("Projetor", sala.Recursos);
        }
        pagina = await client.GetStringAsync("/Admin/Salas/Situacao/1");
        Assert.Equal(HttpStatusCode.Redirect, (await Situacao(client, pagina, false)).StatusCode);
        // Repetir o mesmo envio mantém a sala desativada, sem alternar sua situação.
        Assert.Equal(HttpStatusCode.Redirect, (await Situacao(client, pagina, false)).StatusCode);
        Assert.Contains("Inativa", await client.GetStringAsync("/Admin/Salas"));
        Assert.DoesNotContain("Sala editada", await client.GetStringAsync("/Salas"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Reservas/Criar?salaId=1")).StatusCode);
        pagina = await client.GetStringAsync("/Admin/Salas/Situacao/1");
        Assert.Equal(HttpStatusCode.Redirect, (await Situacao(client, pagina, true)).StatusCode);
        Assert.Contains("Sala editada", await client.GetStringAsync("/Salas"));
        using var consulta = app.Services.CreateScope();
        var contexto = consulta.ServiceProvider.GetRequiredService<ReservaDbContext>();
        Assert.True((await contexto.Reservas.SingleAsync()).Ativa);
        Assert.Equal("Atividade original", (await contexto.Reservas.SingleAsync()).Finalidade);
        var periodo = new PeriodoReserva { Data = new(2099, 1, 1), HoraInicio = new(9, 0), HoraFim = new(10, 0) };
        Assert.Empty(await consulta.ServiceProvider.GetRequiredService<ReservaService>().ConsultarDisponiveisAsync(periodo));
    }

    [Fact]
    public async Task DadosInvalidosNaoAlteramSala()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        var pagina = await client.GetStringAsync("/Admin/Salas/Editar/1");
        var response = await client.PostAsync("/Admin/Salas/Editar/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(pagina),
            ["Cadastro.Identificacao"] = "", ["Cadastro.Localizacao"] = "Bloco D", ["Cadastro.Capacidade"] = "0"
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Equal("Sala original", (await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Salas.SingleAsync()).Identificacao);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Admin/Salas/Editar/999")).StatusCode);
    }

    [Fact]
    public async Task UsuarioComumNaoAcessaNemAlteraAdministracao()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        foreach (var rota in new[] { "/Admin/Salas", "/Admin/Salas/Editar/1", "/Admin/Salas/Situacao/1" })
            Assert.Contains("/Conta/AcessoNegado", (await client.GetAsync(rota)).Headers.Location!.ToString());
        var pagina = await client.GetStringAsync("/Conta/Perfil");
        var response = await Situacao(client, pagina, false);
        Assert.Contains("/Conta/AcessoNegado", response.Headers.Location!.ToString());
        response = await client.PostAsync("/Admin/Salas/Editar/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(pagina),
            ["Cadastro.Identificacao"] = "Indevida", ["Cadastro.Localizacao"] = "D", ["Cadastro.Capacidade"] = "10"
        }));
        Assert.Contains("/Conta/AcessoNegado", response.Headers.Location!.ToString());
        using var scope = app.Services.CreateScope();
        var sala = await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Salas.SingleAsync();
        Assert.True(sala.Ativa);
        Assert.Equal("Sala original", sala.Identificacao);
    }

    [Fact]
    public async Task AlteracaoDeSituacaoExigeTokenEAcaoExplicita()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await Preparar(app);
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        var pagina = await client.GetStringAsync("/Admin/Salas/Situacao/1");
        var response = await client.PostAsync("/Admin/Salas/Situacao/1", new FormUrlEncodedContent(new Dictionary<string, string> { ["ativa"] = "false" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        response = await client.PostAsync("/Admin/Salas/Situacao/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(pagina)
        }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Salas.SingleAsync()).Ativa);
    }

    [PostgresFact]
    public async Task DesativacaoConcorrenteImpedeNovaReservaNoPostgreSql()
    {
        await using var app = new AplicacaoTeste(usarPostgres: true);
        using var client = app.CriarCliente();
        await Preparar(app);
        var options = new DbContextOptionsBuilder<ReservaDbContext>().UseNpgsql(app.ConexaoPostgres).Options;
        await using var dbSala = new ReservaDbContext(options);
        var usuarioId = (await dbSala.Users.FirstAsync()).Id;
        await using var transacao = await dbSala.Database.BeginTransactionAsync();
        await new SalaService(dbSala).AlterarSituacaoAsync(1, false);
        var sinal = new SinalizarBloqueioSala();
        await using var dbReserva = new ReservaDbContext(new DbContextOptionsBuilder<ReservaDbContext>()
            .UseNpgsql(app.ConexaoPostgres).AddInterceptors(sinal).Options);
        var tentativa = new ReservaService(dbReserva).CriarAsync(new CadastroReserva
        {
            SalaId = 1, Data = new(2099, 1, 2), HoraInicio = new(9, 0), HoraFim = new(10, 0), Finalidade = "Nova"
        }, usuarioId);
        await sinal.Solicitado.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(tentativa.IsCompleted);
        await transacao.CommitAsync();
        Assert.NotNull((await tentativa).Erro);
        Assert.Equal(1, await dbSala.Reservas.CountAsync());
    }

    private static Task<HttpResponseMessage> Situacao(HttpClient client, string pagina, bool ativa) =>
        client.PostAsync("/Admin/Salas/Situacao/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(pagina), ["ativa"] = ativa.ToString()
        }));

    private static async Task Preparar(AplicacaoTeste app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
        var usuarioId = (await db.Users.FirstAsync()).Id;
        db.Salas.Add(new Sala { Id = 1, Identificacao = "Sala original", Localizacao = "Bloco C", Capacidade = 30 });
        db.Reservas.Add(new Reserva { SalaId = 1, UsuarioId = usuarioId, Inicio = new(2099, 1, 1, 9, 0, 0),
            Fim = new(2099, 1, 1, 10, 0, 0), Finalidade = "Atividade original" });
        await db.SaveChangesAsync();
    }
}

internal class SinalizarBloqueioSala : DbCommandInterceptor
{
    public TaskCompletionSource Solicitado { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("FOR SHARE")) Solicitado.TrySetResult();
        return ValueTask.FromResult(result);
    }
}

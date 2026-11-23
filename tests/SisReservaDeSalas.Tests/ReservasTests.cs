using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SisReservaDeSalas.Web.Data;
using SisReservaDeSalas.Web.Models;
using SisReservaDeSalas.Web.Services;

namespace SisReservaDeSalas.Tests;

public class ReservasTests
{
    [Fact]
    public async Task ReservaValidaGravaResponsavelERejeitaConflito()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        await CriarSala(app);
        var response = await Reservar(client, "09:00", "10:00");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var confirmacao = await client.GetStringAsync(response.Headers.Location);
        Assert.Contains("Reserva confirmada", confirmacao);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
        var reserva = await db.Reservas.Include(r => r.Usuario).SingleAsync();
        Assert.Equal("usuario@teste.local", reserva.Usuario.Email);
        Assert.NotEqual("id-forjado", reserva.UsuarioId);
        Assert.Equal(HttpStatusCode.OK, (await Reservar(client, "09:30", "10:30")).StatusCode);
        Assert.Equal(1, await db.Reservas.CountAsync());

        using var outro = app.CriarCliente();
        await AutenticacaoTests.Entrar(outro, "admin@teste.local", "SenhaTeste123!");
        Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync(response.Headers.Location)).StatusCode);
    }

    [Theory]
    [InlineData("10:00", "09:00")]
    [InlineData("09:00", "09:00")]
    [InlineData("", "10:00")]
    public async Task PeriodoInvalidoNaoGrava(string inicio, string fim)
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        await CriarSala(app);
        Assert.Equal(HttpStatusCode.OK, (await Reservar(client, inicio, fim)).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Reservas.AnyAsync());
    }

    [Fact]
    public async Task DisponibilidadeConsideraConflitosCanceladasESalasInativas()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        await CriarSala(app);
        await Reservar(client, "09:00", "10:00");
        using var scope = app.Services.CreateScope();
        var servico = scope.ServiceProvider.GetRequiredService<ReservaService>();
        var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
        var periodo = new PeriodoReserva { Data = new(2026, 12, 1), HoraInicio = new(9, 30), HoraFim = new(10, 30) };
        Assert.Empty(await servico.ConsultarDisponiveisAsync(periodo));
        periodo.HoraInicio = new(10, 0);
        Assert.Single(await servico.ConsultarDisponiveisAsync(periodo));
        var paginaDisponibilidade = WebUtility.HtmlDecode(await client.GetStringAsync(
            "/Salas/Disponibilidade?consultar=true&Periodo.Data=2026-12-01&Periodo.HoraInicio=10:00&Periodo.HoraFim=11:00"));
        Assert.Contains("Sala 101", paginaDisponibilidade);
        Assert.Contains("/Reservas/Criar", paginaDisponibilidade);
        var paginaInvalida = WebUtility.HtmlDecode(await client.GetStringAsync(
            "/Salas/Disponibilidade?consultar=true&Periodo.Data=2026-12-01&Periodo.HoraInicio=11:00&Periodo.HoraFim=10:00"));
        Assert.Contains("O horário final deve ser posterior ao inicial.", paginaInvalida);
        var reserva = await db.Reservas.SingleAsync();
        reserva.Ativa = false;
        await db.SaveChangesAsync();
        periodo.HoraInicio = new(9, 30);
        Assert.Single(await servico.ConsultarDisponiveisAsync(periodo));
        var sala = await db.Salas.SingleAsync();
        sala.Ativa = false;
        await db.SaveChangesAsync();
        Assert.Empty(await servico.ConsultarDisponiveisAsync(periodo));
        Assert.Equal(HttpStatusCode.NotFound, (await Reservar(client, "11:00", "12:00")).StatusCode);
    }

    [PostgresFact]
    public async Task PostgreSqlImpedeDuasReservasSimultaneasEPermiteIntervalosConsecutivos()
    {
        await using var app = new AplicacaoTeste(usarPostgres: true);
        using var client = app.CriarCliente();
        await CriarSala(app);
        string usuarioId;
        using (var scope = app.Services.CreateScope())
            usuarioId = (await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Users.FirstAsync()).Id;

        var barreira = new AguardarDuasGravacoes();
        var options = new DbContextOptionsBuilder<ReservaDbContext>()
            .UseNpgsql(app.ConexaoPostgres).AddInterceptors(barreira).Options;
        await using var db1 = new ReservaDbContext(options);
        await using var db2 = new ReservaDbContext(options);
        var cadastro = new CadastroReserva
        {
            SalaId = 1, Data = new(2026, 12, 1), HoraInicio = new(9, 0), HoraFim = new(10, 0), Finalidade = "Aula"
        };
        var resultados = await Task.WhenAll(new ReservaService(db1).CriarAsync(cadastro, usuarioId),
            new ReservaService(db2).CriarAsync(cadastro, usuarioId));
        Assert.Single(resultados, r => r.Id.HasValue);
        Assert.Single(resultados, r => r.Erro is not null);
        using var consulta = app.Services.CreateScope();
        var db = consulta.ServiceProvider.GetRequiredService<ReservaDbContext>();
        Assert.Equal(1, await db.Reservas.CountAsync());
        cadastro.HoraInicio = new(10, 0);
        cadastro.HoraFim = new(11, 0);
        Assert.NotNull((await new ReservaService(db).CriarAsync(cadastro, usuarioId)).Id);
        var primeira = await db.Reservas.OrderBy(r => r.Inicio).FirstAsync();
        primeira.Ativa = false;
        await db.SaveChangesAsync();
        cadastro.HoraInicio = new(9, 0);
        cadastro.HoraFim = new(10, 0);
        Assert.NotNull((await new ReservaService(db).CriarAsync(cadastro, usuarioId)).Id);
    }

    private static async Task CriarSala(AplicacaoTeste app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
        db.Salas.Add(new Sala { Id = 1, Identificacao = "Sala 101", Localizacao = "Bloco C", Capacidade = 30 });
        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> Reservar(HttpClient client, string inicio, string fim)
    {
        var pagina = await client.GetAsync("/Reservas/Criar?salaId=1");
        if (!pagina.IsSuccessStatusCode) return pagina;
        return await client.PostAsync("/Reservas/Criar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(await pagina.Content.ReadAsStringAsync()),
            ["Cadastro.SalaId"] = "1", ["Cadastro.Data"] = "2026-12-01",
            ["Cadastro.HoraInicio"] = inicio, ["Cadastro.HoraFim"] = fim,
            ["Cadastro.Finalidade"] = "Aula", ["Cadastro.UsuarioId"] = "id-forjado"
        }));
    }
}

public class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESERVAS_TEST_POSTGRES")))
            Skip = "Configure RESERVAS_TEST_POSTGRES para executar no PostgreSQL real.";
    }
}

internal class AguardarDuasGravacoes : SaveChangesInterceptor
{
    private int aguardando;
    private readonly TaskCompletionSource liberar = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<Reserva>().Any(e => e.State == EntityState.Added))
        {
            if (Interlocked.Increment(ref aguardando) == 2) liberar.TrySetResult();
            await liberar.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }
        return result;
    }
}

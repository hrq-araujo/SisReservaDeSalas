using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SisReservaDeSalas.Web.Data;
using SisReservaDeSalas.Web.Models;

namespace SisReservaDeSalas.Tests;

public class SalasTests
{
    [Fact]
    public async Task AdministradorCadastraSalaAtivaEConsultaDetalhes()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        var response = await Cadastrar(client, " Sala 101 ", " Bloco C ", "30");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var detalhes = WebUtility.HtmlDecode(await client.GetStringAsync(response.Headers.Location));
        Assert.Contains("Sala 101", detalhes);
        Assert.Contains("Bloco C", detalhes);
        Assert.Contains("30 pessoas", detalhes);

        using var scope = app.Services.CreateScope();
        var sala = await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Salas.SingleAsync();
        Assert.True(sala.Ativa);
        Assert.Equal("Sala 101", sala.Identificacao);
        Assert.Equal("Bloco C", sala.Localizacao);
        Assert.Null(sala.Recursos);
    }

    [Theory]
    [InlineData("", "Bloco C", "30")]
    [InlineData("   ", "Bloco C", "30")]
    [InlineData("Sala 101", "", "30")]
    [InlineData("Sala 101", "Bloco C", "0")]
    [InlineData("Sala 101", "Bloco C", "-1")]
    [InlineData("Sala 101", "Bloco C", "abc")]
    public async Task CadastroInvalidoNaoGravaSala(string identificacao, string localizacao, string capacidade)
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await AutenticacaoTests.Entrar(client, "admin@teste.local", "SenhaTeste123!");
        Assert.Equal(HttpStatusCode.OK, (await Cadastrar(client, identificacao, localizacao, capacidade)).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Salas.AnyAsync());
    }

    [Fact]
    public async Task UsuarioComumNaoPodeCadastrarNemComPostDireto()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        var get = await client.GetAsync("/Salas/Cadastrar");
        Assert.Contains("/Conta/AcessoNegado", get.Headers.Location!.ToString());
        var post = await Cadastrar(client, "Sala 101", "Bloco C", "30", "/Conta/Perfil");
        Assert.Contains("/Conta/AcessoNegado", post.Headers.Location!.ToString());
        using var scope = app.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Salas.AnyAsync());
    }

    [Fact]
    public async Task ListagemEDetalhesExibemSomenteSalasAtivas()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReservaDbContext>();
            db.Salas.AddRange(
                new Sala { Id = 1, Identificacao = "Sala visível", Localizacao = "Bloco C", Capacidade = 30 },
                new Sala { Id = 2, Identificacao = "Sala oculta", Localizacao = "Bloco C", Capacidade = 30, Ativa = false });
            await db.SaveChangesAsync();
        }
        await AutenticacaoTests.Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        var lista = WebUtility.HtmlDecode(await client.GetStringAsync("/Salas"));
        Assert.Contains("Sala visível", lista);
        Assert.DoesNotContain("Sala oculta", lista);
        Assert.DoesNotContain("Cadastrar sala", lista);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Salas/Detalhes/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Salas/Detalhes/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Salas/Detalhes/999")).StatusCode);
    }

    [Fact]
    public async Task VisitanteNaoAcessaSalasSemEntrar()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        var response = await client.GetAsync("/Salas");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Entrar", response.Headers.Location!.ToString());
    }

    private static async Task<HttpResponseMessage> Cadastrar(HttpClient client,
        string identificacao, string localizacao, string capacidade, string formulario = "/Salas/Cadastrar")
    {
        var pagina = await client.GetStringAsync(formulario);
        return await client.PostAsync("/Salas/Cadastrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = AutenticacaoTests.Token(pagina),
            ["Cadastro.Identificacao"] = identificacao,
            ["Cadastro.Localizacao"] = localizacao,
            ["Cadastro.Capacidade"] = capacidade,
            // Campos extras não devem permitir alterar a identidade ou o estado inicial.
            ["Cadastro.Id"] = "999", ["Cadastro.Ativa"] = "false"
        }));
    }
}

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SisReservaDeSalas.Web.Data;

namespace SisReservaDeSalas.Tests;

public class AutenticacaoTests
{
    [Fact]
    public async Task VisitantePrecisaEntrarParaVerPerfil()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        var response = await client.GetAsync("/Conta/Perfil");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Entrar", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("usuario@teste.local", "Usuário")]
    [InlineData("admin@teste.local", "Administrador")]
    public async Task ContaValidaEntraComSeuPerfilESai(string email, string perfil)
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        var response = await Entrar(client, email, "SenhaTeste123!");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var pagina = await client.GetStringAsync("/Conta/Perfil");
        Assert.Contains(perfil, WebUtility.HtmlDecode(pagina));
        var saida = await client.PostAsync("/Conta/Sair", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina)
        }));
        Assert.Equal(HttpStatusCode.Redirect, saida.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Conta/Perfil")).StatusCode);
    }

    [Fact]
    public async Task SenhaInvalidaNaoAutenticaEBloqueiaAposCincoFalhas()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await Entrar(client, "usuario@teste.local", "incorreta")).StatusCode);

        var response = await Entrar(client, "usuario@teste.local", "SenhaTeste123!");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Não foi possível entrar", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Conta/Perfil")).StatusCode);
    }

    [Fact]
    public async Task LoginNaoRedirecionaParaSiteExterno()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        var response = await Entrar(client, "usuario@teste.local", "SenhaTeste123!", "https://exemplo.com");
        Assert.Equal("/Conta/Perfil", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task LoginSemTokenAntifalsificacaoEhRecusado()
    {
        await using var app = new AplicacaoTeste();
        using var client = app.CriarCliente();
        var response = await client.PostAsync("/Conta/Entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Entrada.Email"] = "usuario@teste.local", ["Entrada.Senha"] = "SenhaTeste123!"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    internal static async Task<HttpResponseMessage> Entrar(HttpClient client, string email, string senha, string? retorno = null)
    {
        var pagina = await client.GetStringAsync("/Conta/Entrar");
        return await client.PostAsync("/Conta/Entrar?returnUrl=" + Uri.EscapeDataString(retorno ?? "/Conta/Perfil"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = Token(pagina),
                ["Entrada.Email"] = email, ["Entrada.Senha"] = senha
            }));
    }

    internal static string Token(string html) => WebUtility.HtmlDecode(
        Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
}

internal class AplicacaoTeste : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public HttpClient CriarCliente() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReservaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ReservaDbContext>>();
            connection.Open();
            services.AddDbContext<ReservaDbContext>(options => options.UseSqlite(connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ReservaDbContext>().Database.EnsureCreated();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["UsuariosIniciais:Administrador:Email"] = "admin@teste.local",
            ["UsuariosIniciais:Administrador:Senha"] = "SenhaTeste123!",
            ["UsuariosIniciais:Usuario:Email"] = "usuario@teste.local",
            ["UsuariosIniciais:Usuario:Senha"] = "SenhaTeste123!"
        }).Build();
        PreparacaoUsuarios.ExecutarAsync(scope.ServiceProvider, config).GetAwaiter().GetResult();
        return host;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await connection.DisposeAsync();
    }
}

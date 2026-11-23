using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using SisReservaDeSalas.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<ReservaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReservaDb")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
    .AddEntityFrameworkStores<ReservaDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Entrar";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.Cookie.Name = "SisReservaDeSalas.Autenticacao";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
});

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

var app = builder.Build();

if (args.Contains("--preparar-usuarios"))
{
    using var scope = app.Services.CreateScope();
    await PreparacaoUsuarios.ExecutarAsync(scope.ServiceProvider, app.Configuration);
    Console.WriteLine("Usuários e perfis preparados.");
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }

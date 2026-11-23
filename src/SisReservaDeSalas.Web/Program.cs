using Microsoft.EntityFrameworkCore;
using SisReservaDeSalas.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<ReservaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReservaDb")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

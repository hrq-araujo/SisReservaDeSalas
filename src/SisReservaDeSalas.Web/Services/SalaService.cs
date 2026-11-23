using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SisReservaDeSalas.Web.Data;
using SisReservaDeSalas.Web.Models;

namespace SisReservaDeSalas.Web.Services;

public class SalaService(ReservaDbContext db)
{
    public Task<List<Sala>> ListarAtivasAsync() => db.Salas.AsNoTracking()
        .Where(s => s.Ativa).OrderBy(s => s.Identificacao).ThenBy(s => s.Id).ToListAsync();

    public Task<Sala?> BuscarAtivaAsync(int id) => db.Salas.AsNoTracking()
        .SingleOrDefaultAsync(s => s.Id == id && s.Ativa);

    public async Task<int> CadastrarAsync(CadastroSala cadastro)
    {
        Validator.ValidateObject(cadastro, new ValidationContext(cadastro), validateAllProperties: true);
        var sala = new Sala
        {
            Identificacao = cadastro.Identificacao.Trim(),
            Localizacao = cadastro.Localizacao.Trim(),
            Capacidade = cadastro.Capacidade,
            Recursos = string.IsNullOrWhiteSpace(cadastro.Recursos) ? null : cadastro.Recursos.Trim(),
            Ativa = true
        };
        db.Salas.Add(sala);
        await db.SaveChangesAsync();
        return sala.Id;
    }
}

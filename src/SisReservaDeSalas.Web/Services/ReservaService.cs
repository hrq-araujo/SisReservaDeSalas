using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SisReservaDeSalas.Web.Data;
using SisReservaDeSalas.Web.Models;

namespace SisReservaDeSalas.Web.Services;

public class ReservaService(ReservaDbContext db)
{
    public async Task<List<Sala>> ConsultarDisponiveisAsync(PeriodoReserva periodo)
    {
        Validator.ValidateObject(periodo, new ValidationContext(periodo), true);
        var inicio = periodo.ObterInicio();
        var fim = periodo.ObterFim();
        return await db.Salas.AsNoTracking().Where(s => s.Ativa && !db.Reservas.Any(r =>
                r.SalaId == s.Id && r.Ativa && r.Inicio < fim && inicio < r.Fim))
            .OrderBy(s => s.Identificacao).ToListAsync();
    }

    public async Task<(int? Id, string? Erro)> CriarAsync(CadastroReserva cadastro, string usuarioId)
    {
        Validator.ValidateObject(cadastro, new ValidationContext(cadastro), true);
        ArgumentException.ThrowIfNullOrWhiteSpace(usuarioId);
        if (!await db.Salas.AnyAsync(s => s.Id == cadastro.SalaId && s.Ativa))
            return (null, "A sala não está disponível para reserva.");

        var inicio = cadastro.ObterInicio();
        var fim = cadastro.ObterFim();
        if (await db.Reservas.AnyAsync(r => r.SalaId == cadastro.SalaId && r.Ativa && r.Inicio < fim && inicio < r.Fim))
            return (null, "A sala já possui uma reserva nesse horário. Consulte outro período.");

        var reserva = new Reserva
        {
            SalaId = cadastro.SalaId, UsuarioId = usuarioId,
            Inicio = inicio, Fim = fim, Finalidade = cadastro.Finalidade.Trim()
        };
        db.Reservas.Add(reserva);
        try
        {
            await db.SaveChangesAsync();
            return (reserva.Id, null);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: "EX_Reservas_SemSobreposicao" })
        {
            // O banco é a proteção final quando duas confirmações chegam juntas.
            db.Entry(reserva).State = EntityState.Detached;
            return (null, "Outra pessoa reservou a sala nesse horário. Consulte outro período.");
        }
    }

    public Task<Reserva?> BuscarDoUsuarioAsync(int id, string usuarioId) =>
        db.Reservas.AsNoTracking().Include(r => r.Sala)
            .SingleOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

    public Task<List<Reserva>> ListarDoUsuarioAsync(string usuarioId) =>
        db.Reservas.AsNoTracking().Include(r => r.Sala)
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.Inicio).ThenByDescending(r => r.Id).ToListAsync();

    public async Task<bool> CancelarAsync(int id, string usuarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuarioId);
        // A condição de autoria faz parte do próprio UPDATE, inclusive em requisições simultâneas.
        var alteradas = await db.Reservas.Where(r => r.Id == id && r.UsuarioId == usuarioId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Ativa, false));
        return alteradas > 0;
    }
}

namespace SisReservaDeSalas.Web.Models;

public record ReservaAdministrativa(
    int Id, string Sala, string Responsavel, DateTime Inicio, DateTime Fim, string Finalidade, bool Ativa);

using Microsoft.AspNetCore.Identity;

namespace SisReservaDeSalas.Web.Models;

public class Reserva
{
    public int Id { get; set; }
    public int SalaId { get; set; }
    public Sala Sala { get; set; } = null!;
    public string UsuarioId { get; set; } = string.Empty;
    public IdentityUser Usuario { get; set; } = null!;
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string Finalidade { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
}

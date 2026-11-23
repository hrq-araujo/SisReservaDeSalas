namespace SisReservaDeSalas.Web.Models;

public class Sala
{
    public int Id { get; set; }
    public string Identificacao { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public int Capacidade { get; set; }
    public string? Recursos { get; set; }
    public bool Ativa { get; set; } = true;
}

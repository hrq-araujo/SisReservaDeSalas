using System.ComponentModel.DataAnnotations;

namespace SisReservaDeSalas.Web.Models;

public class CadastroSala
{
    [Required(ErrorMessage = "Informe a identificação da sala.")]
    [StringLength(100, ErrorMessage = "A identificação deve ter até 100 caracteres.")]
    public string Identificacao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a localização.")]
    [StringLength(150, ErrorMessage = "A localização deve ter até 150 caracteres.")]
    public string Localizacao { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A capacidade deve ser maior que zero.")]
    public int Capacidade { get; set; }

    [StringLength(500, ErrorMessage = "Os recursos devem ter até 500 caracteres.")]
    public string? Recursos { get; set; }
}

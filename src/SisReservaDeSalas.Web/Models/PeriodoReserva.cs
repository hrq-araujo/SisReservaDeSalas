using System.ComponentModel.DataAnnotations;

namespace SisReservaDeSalas.Web.Models;

public class PeriodoReserva : IValidatableObject
{
    [Required(ErrorMessage = "Informe a data.")]
    [DataType(DataType.Date)]
    public DateOnly? Data { get; set; }

    [Required(ErrorMessage = "Informe o horário inicial.")]
    [DataType(DataType.Time)]
    public TimeOnly? HoraInicio { get; set; }

    [Required(ErrorMessage = "Informe o horário final.")]
    [DataType(DataType.Time)]
    public TimeOnly? HoraFim { get; set; }

    public DateTime ObterInicio() => Data!.Value.ToDateTime(HoraInicio!.Value);
    public DateTime ObterFim() => Data!.Value.ToDateTime(HoraFim!.Value);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HoraInicio.HasValue && HoraFim.HasValue && HoraFim <= HoraInicio)
            yield return new ValidationResult("O horário final deve ser posterior ao inicial.", [nameof(HoraFim)]);
    }
}

public class CadastroReserva : PeriodoReserva
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma sala.")]
    public int SalaId { get; set; }

    [Required(ErrorMessage = "Informe a finalidade da reserva.")]
    [StringLength(200, ErrorMessage = "A finalidade deve ter até 200 caracteres.")]
    public string Finalidade { get; set; } = string.Empty;
}

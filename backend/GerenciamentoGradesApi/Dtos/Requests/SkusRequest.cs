using System.ComponentModel.DataAnnotations;

namespace GerenciamentoGradesApi.Dtos.Requests;

public record SkusRequest(
    [Required(ErrorMessage = "Informe ao menos um SKU.")]
    [MinLength(1, ErrorMessage = "Informe ao menos um SKU.")]
    List<string> Skus);

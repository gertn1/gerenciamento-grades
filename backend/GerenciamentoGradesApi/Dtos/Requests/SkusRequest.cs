using System.ComponentModel.DataAnnotations;

namespace GerenciamentoGradesApi.Dtos.Requests;

public class SkusRequest
{
    [Required(ErrorMessage = "Informe ao menos um SKU.")]
    [MinLength(1, ErrorMessage = "Informe ao menos um SKU.")]
    public List<string> Skus { get; set; } = [];
}

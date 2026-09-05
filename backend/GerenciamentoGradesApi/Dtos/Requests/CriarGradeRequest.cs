using System.ComponentModel.DataAnnotations;

namespace GerenciamentoGradesApi.Dtos.Requests;

public class CriarGradeRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(80, ErrorMessage = "Nome deve ter no máximo 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sigla é obrigatória.")]
    [MaxLength(15, ErrorMessage = "Sigla deve ter no máximo 15 caracteres.")]
    public string Sigla { get; set; } = string.Empty;
}

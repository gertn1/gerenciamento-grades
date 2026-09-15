using System.ComponentModel.DataAnnotations;

namespace GerenciamentoGradesApi.Dtos.Requests;

public record CriarGradeRequest(
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(80, ErrorMessage = "Nome deve ter no máximo 80 caracteres.")]
    string Nome,

    [Required(ErrorMessage = "Sigla é obrigatória.")]
    [MaxLength(15, ErrorMessage = "Sigla deve ter no máximo 15 caracteres.")]
    string Sigla);

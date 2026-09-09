namespace GerenciamentoGradesApi.Dtos.Responses;

public class AtualizarSkusResponse
{
    public GradeDetalheResponse Grade { get; set; } = new();
    public List<string> SkusInvalidos { get; set; } = [];
}

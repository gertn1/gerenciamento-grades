namespace GerenciamentoGradesApi.Dtos.Responses;

public class GradeDetalheResponse
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public List<SkuResumoResponse> Skus { get; set; } = [];
}

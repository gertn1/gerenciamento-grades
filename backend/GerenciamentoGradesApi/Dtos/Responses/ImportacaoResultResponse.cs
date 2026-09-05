namespace GerenciamentoGradesApi.Dtos.Responses;

public class ImportacaoResultResponse
{
    public int TotalLinhas { get; set; }
    public int Sucesso { get; set; }
    public List<ErroLinhaResponse> Erros { get; set; } = [];
}

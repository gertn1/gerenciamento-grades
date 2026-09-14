namespace GerenciamentoGradesApi.Dtos.Responses;

public class SkusOrfaosResponse
{
    public List<SkuResumoResponse> Itens { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

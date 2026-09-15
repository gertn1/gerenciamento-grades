namespace GerenciamentoGradesApi.Dtos.Responses;

public record SkusOrfaosResponse(List<SkuResumoResponse> Itens, int Total, int Pagina, int TamanhoPagina);

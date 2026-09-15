namespace GerenciamentoGradesApi.Dtos.Responses;

public record ImportacaoResultResponse(int TotalLinhas, int Sucesso, List<ErroLinhaResponse> Erros);

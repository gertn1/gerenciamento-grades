namespace GerenciamentoGradesApi.Dtos.Responses;

public record GradeDetalheResponse(int CodigoGrade, string Nome, string Sigla, List<SkuResumoResponse> Skus);

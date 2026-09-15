namespace GerenciamentoGradesApi.Dtos.Responses;

public record AtualizarSkusResponse(GradeDetalheResponse Grade, List<SkuRejeitadoResponse> SkusRejeitados);

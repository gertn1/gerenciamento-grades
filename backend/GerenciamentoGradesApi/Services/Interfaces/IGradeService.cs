using GerenciamentoGradesApi.Dtos.Requests;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Services.Resultados;

namespace GerenciamentoGradesApi.Services.Interfaces;

public interface IGradeService
{
    Task<IEnumerable<GradeListItemResponse>> ListarAsync(int? codigoGrade, string? nome);

    // null = grade não encontrada.
    Task<GradeDetalheResponse?> ObterDetalheAsync(int codigoGrade);

    // null = grade não encontrada; lista vazia = encontrada, sem resultados para o termo.
    Task<IEnumerable<SkuResumoResponse>?> BuscarSkusDisponiveisAsync(int codigoGrade, string? termo);

    // Diagnóstico: produtos sem grade (paginado) e grades sem nenhum SKU.
    Task<SkusOrfaosResponse> ListarSkusOrfaosAsync(string? termo, int pagina, int tamanhoPagina);

    Task<IEnumerable<GradeListItemResponse>> ListarGradesVaziasAsync(int? codigoGrade, string? nome);

    Task<ResultadoOperacao<GradeResponse>> CriarAsync(CriarGradeRequest request, string matricula);

    Task<ResultadoOperacao<GradeResponse>> AtualizarAsync(int codigoGrade, AtualizarGradeRequest request, string matricula);

    // false = grade não encontrada.
    Task<bool> ExcluirAsync(int codigoGrade, string matricula);

    // null = grade não encontrada.
    Task<AtualizarSkusResponse?> AdicionarSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula);

    // null = grade não encontrada.
    Task<AtualizarSkusResponse?> RemoverSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula);
}

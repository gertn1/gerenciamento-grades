using GerenciamentoGradesApi.Dtos.Requests;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Services.Resultados;

namespace GerenciamentoGradesApi.Services.Interfaces;

public interface IGradeService
{
    Task<IEnumerable<GradeListItemResponse>> ListarAsync(int? codigo, string? nome);

    // null = grade não encontrada.
    Task<GradeDetalheResponse?> ObterDetalheAsync(int codigo);

    // null = grade não encontrada; lista vazia = encontrada, sem resultados para o termo.
    Task<IEnumerable<SkuResumoResponse>?> BuscarSkusDisponiveisAsync(int codigo, string? termo);

    Task<ResultadoOperacao<GradeResponse>> CriarAsync(CriarGradeRequest request, string matricula);

    Task<ResultadoOperacao<GradeResponse>> AtualizarAsync(int codigo, AtualizarGradeRequest request, string matricula);

    // false = grade não encontrada.
    Task<bool> ExcluirAsync(int codigo, string matricula);

    // null = grade não encontrada.
    Task<AtualizarSkusResponse?> AdicionarSkusAsync(int codigo, IEnumerable<string> skus, string matricula);

    // null = grade não encontrada.
    Task<AtualizarSkusResponse?> RemoverSkusAsync(int codigo, IEnumerable<string> skus, string matricula);
}

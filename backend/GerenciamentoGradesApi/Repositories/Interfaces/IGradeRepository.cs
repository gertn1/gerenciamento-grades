using GerenciamentoGradesApi.Models;

namespace GerenciamentoGradesApi.Repositories.Interfaces;

public interface IGradeRepository
{
    Task<IEnumerable<GradeListItem>> ListarAsync(int? codigo, string? nome);

    Task<Grade?> ObterPorCodigoAsync(int codigo);

    Task<IEnumerable<SkuResumo>> ListarSkusPorGradeAsync(int codigo);

    Task<IEnumerable<SkuResumo>> BuscarSkusDisponiveisAsync(string termo, int gradeCodigoAtual);

    Task<int?> ObterCodigoPorNomeAsync(string nome);

    Task<int> CriarAsync(string nome, string sigla, string matricula);

    Task<bool> AtualizarAsync(int codigo, string nome, string sigla, string matricula);

    Task<bool> ExcluirAsync(int codigo, string matricula);

    Task<HashSet<string>> FiltrarSkusExistentesAsync(IEnumerable<string> skus);

    Task VincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula);

    Task<HashSet<string>> FiltrarSkusVinculadosAsync(int codigo, IEnumerable<string> skus);

    Task DesvincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula);
}

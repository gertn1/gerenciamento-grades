using GerenciamentoGradesApi.Models;

namespace GerenciamentoGradesApi.Repositories.Interfaces;

public interface IGradeRepository
{
    Task<IEnumerable<GradeListItem>> ListarAsync(int? codigo, string? nome);

    Task<Grade?> ObterPorCodigoAsync(int codigo);

    // Usado para bloquear nome/sigla duplicados antes de criar/editar. `codigoExcluido`
    // ignora a própria grade sendo editada (ela pode manter seu nome/sigla atual).
    Task<Grade?> ObterPorNomeOuSiglaAsync(string nome, string sigla, int? codigoExcluido = null);

    Task<IEnumerable<SkuResumo>> ListarSkusPorGradeAsync(int codigo);

    Task<IEnumerable<SkuResumo>> BuscarSkusDisponiveisAsync(string termo, int gradeCodigoAtual);

    Task<int?> ObterCodigoPorNomeAsync(string nome);

    Task<int> CriarAsync(string nome, string sigla, string matricula);

    Task<bool> AtualizarAsync(int codigo, string nome, string sigla, string matricula);

    Task<bool> ExcluirAsync(int codigo, string matricula);

    Task<HashSet<string>> FiltrarSkusExistentesAsync(IEnumerable<string> skus);

    Task<Dictionary<string, SkuVinculo>> ObterVinculoAtualAsync(IEnumerable<string> skus);

    Task VincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula);

    Task<HashSet<string>> FiltrarSkusVinculadosAsync(int codigo, IEnumerable<string> skus);

    Task DesvincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula);
}

using GerenciamentoGradesApi.Models;

namespace GerenciamentoGradesApi.Repositories.Interfaces;

public interface IGradeRepository
{
    Task<IEnumerable<GradeListItem>> ListarAsync(int? codigoGrade, string? nome);

    Task<Grade?> ObterPorCodigoAsync(int codigoGrade);

    // Usado para bloquear nome/sigla duplicados antes de criar/editar. `codigoGradeExcluida`
    // ignora a própria grade sendo editada (ela pode manter seu nome/sigla atual).
    Task<Grade?> ObterPorNomeOuSiglaAsync(string nome, string sigla, int? codigoGradeExcluida = null);

    Task<IEnumerable<SkuResumo>> ListarSkusPorGradeAsync(int codigoGrade);

    Task<IEnumerable<SkuResumo>> BuscarSkusDisponiveisAsync(string termo, int codigoGradeAtual);

    // Diagnóstico: produtos sem nenhuma grade vinculada (paginado — a tabela
    // de produtos é grande) e grades cadastradas sem nenhum SKU vinculado.
    Task<(IEnumerable<SkuResumo> Itens, int Total)> ListarSkusOrfaosAsync(string? termo, int pagina, int tamanhoPagina);

    // Todos os SKUs órfãos, lidos sob demanda do banco (sem paginação e sem
    // carregar a lista inteira em memória) — usado pela exportação em CSV.
    IAsyncEnumerable<SkuResumo> ListarTodosSkusOrfaosAsync(string? termo);

    Task<IEnumerable<GradeListItem>> ListarGradesVaziasAsync(int? codigoGrade, string? nome);

    Task<Grade?> ObterPorNomeAsync(string nome);

    Task<int> CriarAsync(string nome, string sigla, string matricula);

    Task<bool> AtualizarAsync(int codigoGrade, string nome, string sigla, string matricula);

    Task<bool> ExcluirAsync(int codigoGrade, string matricula);

    Task<HashSet<string>> FiltrarSkusExistentesAsync(IEnumerable<string> skus);

    Task<Dictionary<string, SkuVinculo>> ObterVinculoAtualAsync(IEnumerable<string> skus);

    Task VincularSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula);

    Task<HashSet<string>> FiltrarSkusVinculadosAsync(int codigoGrade, IEnumerable<string> skus);

    Task DesvincularSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula);
}

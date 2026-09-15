using GerenciamentoGradesApi.Dtos.Requests;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Models;
using GerenciamentoGradesApi.Repositories.Interfaces;
using GerenciamentoGradesApi.Services.Interfaces;
using GerenciamentoGradesApi.Services.Resultados;
namespace GerenciamentoGradesApi.Services;

public class GradeService(IGradeRepository gradeRepository) : IGradeService
{
    public async Task<IEnumerable<GradeListItemResponse>> ListarAsync(int? codigoGrade, string? nome)
    {
        var grades = await gradeRepository.ListarAsync(codigoGrade, nome);
        return grades.Select(MapearGradeListItem);
    }

    public async Task<GradeDetalheResponse?> ObterDetalheAsync(int codigoGrade)
    {
        var grade = await gradeRepository.ObterPorCodigoAsync(codigoGrade);
        if (grade is null)
            return null;

        var skus = await gradeRepository.ListarSkusPorGradeAsync(codigoGrade);

        return new GradeDetalheResponse(grade.Codigo, grade.Nome, grade.Sigla, skus.Select(MapearSku).ToList());
    }

    public async Task<IEnumerable<SkuResumoResponse>?> BuscarSkusDisponiveisAsync(int codigoGrade, string? termo)
    {
        var skus = await gradeRepository.BuscarSkusDisponiveisAsync(termo?.Trim() ?? string.Empty, codigoGrade);
        return skus.Select(MapearSku);
    }

    public async Task<SkusOrfaosResponse> ListarSkusOrfaosAsync(string? termo, int pagina, int tamanhoPagina)
    {
        var (itens, total) = await gradeRepository.ListarSkusOrfaosAsync(termo?.Trim(), pagina, tamanhoPagina);

        return new SkusOrfaosResponse(itens.Select(MapearSku).ToList(), total, pagina, tamanhoPagina);
    }

    public async Task<IEnumerable<GradeListItemResponse>> ListarGradesVaziasAsync(int? codigoGrade, string? nome)
    {
        var grades = await gradeRepository.ListarGradesVaziasAsync(codigoGrade, nome?.Trim());
        return grades.Select(MapearGradeListItem);
    }

    public async Task<ResultadoOperacao<GradeResponse>> CriarAsync(CriarGradeRequest request, string matricula)
    {
        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim();

        var existente = await gradeRepository.ObterPorNomeOuSiglaAsync(nome, sigla);
        if (existente is not null)
            return ResultadoOperacao<GradeResponse>.Conflito("Já existe uma grade com esse nome ou sigla.");

        var codigoGrade = await gradeRepository.CriarAsync(nome, sigla, matricula);
        return ResultadoOperacao<GradeResponse>.ComSucesso(new GradeResponse(codigoGrade, nome, sigla));
    }

    public async Task<ResultadoOperacao<GradeResponse>> AtualizarAsync(int codigoGrade, AtualizarGradeRequest request, string matricula)
    {
        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim();

        // Ignora a própria grade na busca por duplicidade: ela pode ser salva
        // sem alterar nome/sigla, ou trocando só um dos dois campos.
        var existente = await gradeRepository.ObterPorNomeOuSiglaAsync(nome, sigla, codigoGrade);
        if (existente is not null)
            return ResultadoOperacao<GradeResponse>.Conflito("Já existe uma grade com esse nome ou sigla.");

        var atualizado = await gradeRepository.AtualizarAsync(codigoGrade, nome, sigla, matricula);
        if (!atualizado)
            return ResultadoOperacao<GradeResponse>.NaoEncontrado($"Grade {codigoGrade} não encontrada.");

        return ResultadoOperacao<GradeResponse>.ComSucesso(new GradeResponse(codigoGrade, nome, sigla));
    }

    public Task<bool> ExcluirAsync(int codigoGrade, string matricula) => gradeRepository.ExcluirAsync(codigoGrade, matricula);

    public async Task<AtualizarSkusResponse?> AdicionarSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula)
    {
        var grade = await gradeRepository.ObterPorCodigoAsync(codigoGrade);
        if (grade is null)
            return null;

        var skusDistintos = skus.Distinct().ToList();
        var existentes = await gradeRepository.FiltrarSkusExistentesAsync(skusDistintos);

        var rejeitados = skusDistintos
            .Where(s => !existentes.Contains(s))
            .Select(s => new SkuRejeitadoResponse(s, $"SKU {s} não encontrado."))
            .ToList();

        // Regra de negócio: um SKU só pode ser vinculado se estiver livre (sem
        // grade) ou já pertencer à própria grade que está sendo editada — não
        // pode "roubar" um SKU de outra grade por aqui.
        var vinculos = await gradeRepository.ObterVinculoAtualAsync(existentes);
        var paraVincular = new List<string>();

        foreach (var sku in existentes)
        {
            var vinculo = vinculos.GetValueOrDefault(sku);

            if (vinculo?.CodigoGrade is int codigoGradeAtual && codigoGradeAtual != codigoGrade)
            {
                rejeitados.Add(new SkuRejeitadoResponse(
                    sku, $"SKU {sku} já está vinculado à grade {codigoGradeAtual} - {vinculo.NomeGrade}."));
                continue;
            }

            if (vinculo?.CodigoGrade == codigoGrade)
                continue;

            paraVincular.Add(sku);
        }

        if (paraVincular.Count > 0)
            await gradeRepository.VincularSkusAsync(codigoGrade, paraVincular, matricula);

        var detalhe = await ObterDetalheAsync(codigoGrade);
        return new AtualizarSkusResponse(detalhe!, rejeitados);
    }

    public async Task<AtualizarSkusResponse?> RemoverSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula)
    {
        var grade = await gradeRepository.ObterPorCodigoAsync(codigoGrade);
        if (grade is null)
            return null;

        var skusDistintos = skus.Distinct().ToList();
        var vinculados = await gradeRepository.FiltrarSkusVinculadosAsync(codigoGrade, skusDistintos);

        var rejeitados = skusDistintos
            .Where(s => !vinculados.Contains(s))
            .Select(s => new SkuRejeitadoResponse(s, $"SKU {s} não está vinculado a esta grade."))
            .ToList();

        if (vinculados.Count > 0)
            await gradeRepository.DesvincularSkusAsync(codigoGrade, vinculados, matricula);

        var detalhe = await ObterDetalheAsync(codigoGrade);
        return new AtualizarSkusResponse(detalhe!, rejeitados);
    }

    private static GradeListItemResponse MapearGradeListItem(GradeListItem grade) =>
        new(grade.CodigoGrade, grade.Nome, grade.Sigla, grade.QtdSkus);

    private static SkuResumoResponse MapearSku(SkuResumo sku) => new(sku.CodigoSku, sku.Descricao);
}

using GerenciamentoGradesApi.Dtos.Requests;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Models;
using GerenciamentoGradesApi.Repositories.Interfaces;
using GerenciamentoGradesApi.Services.Interfaces;
using GerenciamentoGradesApi.Services.Resultados;

namespace GerenciamentoGradesApi.Services;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _gradeRepository;

    public GradeService(IGradeRepository gradeRepository)
    {
        _gradeRepository = gradeRepository;
    }

    public async Task<IEnumerable<GradeListItemResponse>> ListarAsync(int? codigo, string? nome)
    {
        var grades = await _gradeRepository.ListarAsync(codigo, nome);

        return grades.Select(g => new GradeListItemResponse
        {
            Codigo = g.Codigo,
            Nome = g.Nome,
            Sigla = g.Sigla,
            QtdSkus = g.QtdSkus
        });
    }

    public async Task<GradeDetalheResponse?> ObterDetalheAsync(int codigo)
    {
        var grade = await _gradeRepository.ObterPorCodigoAsync(codigo);
        if (grade is null)
            return null;

        var skus = await _gradeRepository.ListarSkusPorGradeAsync(codigo);

        return new GradeDetalheResponse
        {
            Codigo = grade.Codigo,
            Nome = grade.Nome,
            Sigla = grade.Sigla,
            Skus = skus.Select(MapearSku).ToList()
        };
    }

    public async Task<IEnumerable<SkuResumoResponse>?> BuscarSkusDisponiveisAsync(int codigo, string? termo)
    {
        var skus = await _gradeRepository.BuscarSkusDisponiveisAsync(termo?.Trim() ?? string.Empty, codigo);
        return skus.Select(MapearSku);
    }

    public async Task<SkusOrfaosResponse> ListarSkusOrfaosAsync(int pagina, int tamanhoPagina)
    {
        var (itens, total) = await _gradeRepository.ListarSkusOrfaosAsync(pagina, tamanhoPagina);

        return new SkusOrfaosResponse
        {
            Itens = itens.Select(MapearSku).ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }

    public async Task<IEnumerable<GradeListItemResponse>> ListarGradesVaziasAsync()
    {
        var grades = await _gradeRepository.ListarGradesVaziasAsync();

        return grades.Select(g => new GradeListItemResponse
        {
            Codigo = g.Codigo,
            Nome = g.Nome,
            Sigla = g.Sigla,
            QtdSkus = g.QtdSkus
        });
    }

    public async Task<ResultadoOperacao<GradeResponse>> CriarAsync(CriarGradeRequest request, string matricula)
    {
        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim();

        var existente = await _gradeRepository.ObterPorNomeOuSiglaAsync(nome, sigla);
        if (existente is not null)
            return ResultadoOperacao<GradeResponse>.Conflito("Já existe uma grade com esse nome ou sigla.");

        var codigo = await _gradeRepository.CriarAsync(nome, sigla, matricula);
        return ResultadoOperacao<GradeResponse>.ComSucesso(new GradeResponse { Codigo = codigo, Nome = nome, Sigla = sigla });
    }

    public async Task<ResultadoOperacao<GradeResponse>> AtualizarAsync(int codigo, AtualizarGradeRequest request, string matricula)
    {
        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim();

        // Ignora a própria grade na busca por duplicidade: ela pode ser salva
        // sem alterar nome/sigla, ou trocando só um dos dois campos.
        var existente = await _gradeRepository.ObterPorNomeOuSiglaAsync(nome, sigla, codigo);
        if (existente is not null)
            return ResultadoOperacao<GradeResponse>.Conflito("Já existe uma grade com esse nome ou sigla.");

        var atualizado = await _gradeRepository.AtualizarAsync(codigo, nome, sigla, matricula);
        if (!atualizado)
            return ResultadoOperacao<GradeResponse>.NaoEncontrado($"Grade {codigo} não encontrada.");

        return ResultadoOperacao<GradeResponse>.ComSucesso(new GradeResponse { Codigo = codigo, Nome = nome, Sigla = sigla });
    }

    public Task<bool> ExcluirAsync(int codigo, string matricula) => _gradeRepository.ExcluirAsync(codigo, matricula);

    public async Task<AtualizarSkusResponse?> AdicionarSkusAsync(int codigo, IEnumerable<string> skus, string matricula)
    {
        var grade = await _gradeRepository.ObterPorCodigoAsync(codigo);
        if (grade is null)
            return null;

        var skusDistintos = skus.Distinct().ToList();
        var existentes = await _gradeRepository.FiltrarSkusExistentesAsync(skusDistintos);

        var rejeitados = skusDistintos
            .Where(s => !existentes.Contains(s))
            .Select(s => new SkuRejeitadoResponse { Sku = s, Mensagem = $"SKU {s} não encontrado." })
            .ToList();

        // Regra de negócio: um SKU só pode ser vinculado se estiver livre (sem
        // grade) ou já pertencer à própria grade que está sendo editada — não
        // pode "roubar" um SKU de outra grade por aqui.
        var vinculos = await _gradeRepository.ObterVinculoAtualAsync(existentes);
        var paraVincular = new List<string>();

        foreach (var sku in existentes)
        {
            var vinculo = vinculos.GetValueOrDefault(sku);

            if (vinculo?.CodigoGrade is int codigoAtual && codigoAtual != codigo)
            {
                rejeitados.Add(new SkuRejeitadoResponse
                {
                    Sku = sku,
                    Mensagem = $"SKU {sku} já está vinculado à grade {codigoAtual} - {vinculo.NomeGrade}."
                });
                continue;
            }

            if (vinculo?.CodigoGrade == codigo)
                continue; // já pertence a esta grade — nada a fazer, mas não é erro

            paraVincular.Add(sku);
        }

        if (paraVincular.Count > 0)
            await _gradeRepository.VincularSkusAsync(codigo, paraVincular, matricula);

        var detalhe = await ObterDetalheAsync(codigo);
        return new AtualizarSkusResponse { Grade = detalhe!, SkusRejeitados = rejeitados };
    }

    public async Task<AtualizarSkusResponse?> RemoverSkusAsync(int codigo, IEnumerable<string> skus, string matricula)
    {
        var grade = await _gradeRepository.ObterPorCodigoAsync(codigo);
        if (grade is null)
            return null;

        var skusDistintos = skus.Distinct().ToList();
        var vinculados = await _gradeRepository.FiltrarSkusVinculadosAsync(codigo, skusDistintos);

        var rejeitados = skusDistintos
            .Where(s => !vinculados.Contains(s))
            .Select(s => new SkuRejeitadoResponse { Sku = s, Mensagem = $"SKU {s} não está vinculado a esta grade." })
            .ToList();

        if (vinculados.Count > 0)
            await _gradeRepository.DesvincularSkusAsync(codigo, vinculados, matricula);

        var detalhe = await ObterDetalheAsync(codigo);
        return new AtualizarSkusResponse { Grade = detalhe!, SkusRejeitados = rejeitados };
    }

    private static SkuResumoResponse MapearSku(SkuResumo sku) => new() { Codigo = sku.Codigo, Descricao = sku.Descricao };
}

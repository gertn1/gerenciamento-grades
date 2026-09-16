using ClosedXML.Excel;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Repositories.Interfaces;
using GerenciamentoGradesApi.Services.Interfaces;
using GerenciamentoGradesApi.Services.Resultados;

namespace GerenciamentoGradesApi.Services;

public class PlanilhaGradeService(IGradeRepository gradeRepository) : IPlanilhaGradeService
{
    public byte[] GerarModeloCriacaoMassiva() => GerarModelo("Grades", "NOME_GRADE", "CODIGO_SKU", "SIGLA");

    public byte[] GerarModeloImportacaoMassivaAtualizacao() => GerarModelo("SKUs", "CODIGO_GRADE", "CODIGO_SKU");

    public byte[] GerarModeloExclusaoMassivaSkus() => GerarModelo("SKUs", "CODIGO_GRADE", "CODIGO_SKU");

    private static byte[] GerarModelo(string nomePlanilha, params string[] colunas)
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add(nomePlanilha);

        for (var coluna = 0; coluna < colunas.Length; coluna++)
            planilha.Cell(1, coluna + 1).Value = colunas[coluna];

        planilha.Range(1, 1, 1, colunas.Length).Style.Font.Bold = true;
        planilha.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // "Criação massiva": planilha NOME_GRADE / CODIGO_SKU / SIGLA. Cria a grade quando
    // o NOME_GRADE ainda não existe.
    public async Task<ResultadoOperacao<ImportacaoResultResponse>> CriarEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula)
    {
        var (linhas, erroLeitura) = LerArquivo(conteudoArquivo, nomeArquivo, LerPlanilhaCriacao);
        if (erroLeitura is not null)
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida(erroLeitura);

        var (linhasValidas, erros) = ValidarLinhasCriacao(linhas);
        var skusExistentes = await gradeRepository.FiltrarSkusExistentesAsync(linhasValidas.Select(l => l.Sku));

        var itensPorGrade = new Dictionary<string, List<LinhaCriacao>>(StringComparer.OrdinalIgnoreCase);

        foreach (var linha in linhasValidas)
        {
            if (!skusExistentes.Contains(linha.Sku))
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, $"SKU {linha.Sku} não encontrado."));
                continue;
            }

            if (!itensPorGrade.TryGetValue(linha.GradeNome, out var lista))
            {
                lista = [];
                itensPorGrade[linha.GradeNome] = lista;
            }

            lista.Add(linha);
        }

        var vinculoEfetivo = await CarregarVinculoEfetivoAsync(skusExistentes);
        var nomesGradeCache = new Dictionary<int, string?>();

        var sucesso = 0;
        foreach (var (gradeNome, itens) in itensPorGrade)
        {
            // A mesma GRADE (nome) não pode aparecer com siglas diferentes dentro
            // do próprio arquivo — sem isso, qual das duas seria usada ficaria
            // arbitrário (a da primeira linha lida).
            var siglasDaGrade = itens.Select(i => i.Sigla).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (siglasDaGrade.Count > 1)
            {
                erros.AddRange(itens.Select(i => new ErroLinhaResponse(
                    i.Linha,
                    $"NOME_GRADE '{gradeNome}' aparece com siglas diferentes na planilha ({string.Join(", ", siglasDaGrade)}).")));
                continue;
            }

            var codigoGrade = await ResolverCodigoGradeAsync(gradeNome, siglasDaGrade[0], matricula, itens, erros);
            if (codigoGrade is null)
                continue;

            sucesso += await AplicarVinculosDoGrupoAsync(
                codigoGrade.Value, itens.Select(i => (i.Linha, i.Sku)).ToList(), vinculoEfetivo, nomesGradeCache, matricula, erros);
        }

        return ResultadoOperacao<ImportacaoResultResponse>.ComSucesso(new ImportacaoResultResponse(linhas.Count, sucesso, erros));
    }

    // "Importação massiva" (atualização): planilha CODIGO_GRADE / CODIGO_SKU.
    // Só vincula SKUs a grades que já existem — nunca cria grade nova.
    public async Task<ResultadoOperacao<ImportacaoResultResponse>> AtualizarEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula)
    {
        var (linhas, erroLeitura) = LerArquivo(conteudoArquivo, nomeArquivo, LerPlanilhaAtualizacao);
        if (erroLeitura is not null)
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida(erroLeitura);

        var erros = new List<ErroLinhaResponse>();
        var linhasValidas = new List<(LinhaAtualizacao Linha, int CodigoGrade)>();

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha.CodigoGrade) || string.IsNullOrWhiteSpace(linha.CodigoSku))
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, "CODIGO_GRADE e CODIGO_SKU são obrigatórios."));
                continue;
            }

            if (!int.TryParse(linha.CodigoGrade, out var codigoGrade))
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, $"CODIGO_GRADE '{linha.CodigoGrade}' inválido — informe o código numérico da grade."));
                continue;
            }

            linhasValidas.Add((linha, codigoGrade));
        }

        var skusExistentes = await gradeRepository.FiltrarSkusExistentesAsync(linhasValidas.Select(l => l.Linha.CodigoSku));

        var itensPorGrade = new Dictionary<int, List<(int Linha, string Sku)>>();

        foreach (var (linha, codigoGrade) in linhasValidas)
        {
            if (!skusExistentes.Contains(linha.CodigoSku))
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, $"SKU {linha.CodigoSku} não encontrado."));
                continue;
            }

            if (!itensPorGrade.TryGetValue(codigoGrade, out var lista))
            {
                lista = [];
                itensPorGrade[codigoGrade] = lista;
            }

            lista.Add((linha.Linha, linha.CodigoSku));
        }

        var vinculoEfetivo = await CarregarVinculoEfetivoAsync(skusExistentes);
        var nomesGradeCache = new Dictionary<int, string?>();

        var sucesso = 0;
        foreach (var (codigoGrade, itens) in itensPorGrade)
        {
            var grade = await gradeRepository.ObterPorCodigoAsync(codigoGrade);
            if (grade is null)
            {
                erros.AddRange(itens.Select(i => new ErroLinhaResponse(
                    i.Linha,
                    $"Grade {codigoGrade} não encontrada — esta importação só atualiza SKUs de grades já existentes.")));
                continue;
            }

            nomesGradeCache[codigoGrade] = grade.Nome;
            sucesso += await AplicarVinculosDoGrupoAsync(codigoGrade, itens, vinculoEfetivo, nomesGradeCache, matricula, erros);
        }

        return ResultadoOperacao<ImportacaoResultResponse>.ComSucesso(new ImportacaoResultResponse(linhas.Count, sucesso, erros));
    }

    private static (List<LinhaCriacao> Validas, List<ErroLinhaResponse> Erros) ValidarLinhasCriacao(List<LinhaCriacao> linhas)
    {
        var erros = new List<ErroLinhaResponse>();
        var validas = new List<LinhaCriacao>();

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha.Sku) || string.IsNullOrWhiteSpace(linha.GradeNome) || string.IsNullOrWhiteSpace(linha.Sigla))
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, "NOME_GRADE, CODIGO_SKU e SIGLA são obrigatórios."));
                continue;
            }

            if (linha.GradeNome.Length > 80)
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, "NOME_GRADE deve ter no máximo 80 caracteres."));
                continue;
            }

            if (linha.Sigla.Length > 35)
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, "SIGLA deve ter no máximo 35 caracteres."));
                continue;
            }

            validas.Add(linha);
        }

        return (validas, erros);
    }

    // Estado atual do vínculo SKU → grade no banco. Durante o processamento é
    // atualizado a cada grupo vinculado, para também pegar duas linhas do MESMO
    // arquivo disputando o mesmo SKU para grades diferentes.
    private async Task<Dictionary<string, int?>> CarregarVinculoEfetivoAsync(IEnumerable<string> skus)
    {
        var vinculos = await gradeRepository.ObterVinculoAtualAsync(skus);
        return vinculos.ToDictionary(kv => kv.Key, kv => kv.Value.CodigoGrade, StringComparer.OrdinalIgnoreCase);
    }

    // Regra de negócio compartilhada pela criação e pela atualização em massa
    // (a mesma de GradeService.AdicionarSkusAsync): um SKU só é vinculado se
    // estiver livre ou já pertencer à grade de destino — nunca "roubado" de
    // outra grade pela planilha. Devolve quantas linhas do grupo deram certo.
    private async Task<int> AplicarVinculosDoGrupoAsync(
        int codigoGrade, List<(int Linha, string Sku)> itens, Dictionary<string, int?> vinculoEfetivo,
        Dictionary<int, string?> nomesGradeCache, string matricula, List<ErroLinhaResponse> erros)
    {
        var bloqueados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paraVincular = new List<string>();

        foreach (var sku in itens.Select(i => i.Sku).Distinct())
        {
            vinculoEfetivo.TryGetValue(sku, out var codigoGradeAtual);

            if (codigoGradeAtual is int codigoGradeVinculada && codigoGradeVinculada != codigoGrade)
                bloqueados.Add(sku);
            else if (codigoGradeAtual != codigoGrade)
                paraVincular.Add(sku);
        }

        if (paraVincular.Count > 0)
        {
            await gradeRepository.VincularSkusAsync(codigoGrade, paraVincular, matricula);
            foreach (var sku in paraVincular)
                vinculoEfetivo[sku] = codigoGrade;
        }

        var sucesso = 0;
        foreach (var (linha, sku) in itens)
        {
            if (!bloqueados.Contains(sku))
            {
                sucesso++;
                continue;
            }

            var codigoGradeBloqueio = vinculoEfetivo.GetValueOrDefault(sku);
            var nomeGradeBloqueio = codigoGradeBloqueio is int cb ? await ObterNomeGradeAsync(cb, nomesGradeCache) : null;

            erros.Add(new ErroLinhaResponse(linha, $"SKU {sku} já está vinculado à grade {codigoGradeBloqueio} - {nomeGradeBloqueio}."));
        }

        return sucesso;
    }

    private async Task<string?> ObterNomeGradeAsync(int codigoGrade, Dictionary<int, string?> nomesGradeCache)
    {
        if (!nomesGradeCache.TryGetValue(codigoGrade, out var nome))
        {
            var grade = await gradeRepository.ObterPorCodigoAsync(codigoGrade);
            nome = grade?.Nome;
            nomesGradeCache[codigoGrade] = nome;
        }

        return nome;
    }

    // Resolve o código da grade de destino de um grupo da criação massiva
    // (criando a grade quando o nome ainda não existe), ou registra o(s)
    // erro(s) e devolve null quando o grupo inteiro deve ser rejeitado (sigla
    // divergente ou sigla já usada por outra grade).
    private async Task<int?> ResolverCodigoGradeAsync(
        string gradeNome, string siglaGrade, string matricula, List<LinhaCriacao> itens, List<ErroLinhaResponse> erros)
    {
        var gradeExistente = await gradeRepository.ObterPorNomeAsync(gradeNome);

        if (gradeExistente is not null)
        {
            // A grade já existe — a sigla da planilha não é usada pra
            // alterá-la silenciosamente; se divergir da sigla real, é erro.
            if (!string.Equals(gradeExistente.Sigla, siglaGrade, StringComparison.OrdinalIgnoreCase))
            {
                erros.AddRange(itens.Select(i => new ErroLinhaResponse(
                    i.Linha,
                    $"NOME_GRADE '{gradeNome}' já existe com a sigla '{gradeExistente.Sigla}' — a sigla informada '{siglaGrade}' diverge.")));
                return null;
            }

            return gradeExistente.Codigo;
        }

        // Grade nova — a sigla não pode colidir com a de uma grade diferente
        // já existente (mesma regra aplicada na criação individual, ver
        // GradeService.CriarAsync).
        var conflito = await gradeRepository.ObterPorNomeOuSiglaAsync(gradeNome, siglaGrade);
        if (conflito is not null)
        {
            erros.AddRange(itens.Select(i => new ErroLinhaResponse(
                i.Linha,
                $"SIGLA '{siglaGrade}' já está em uso pela grade {conflito.Codigo} - {conflito.Nome}.")));
            return null;
        }

        return await gradeRepository.CriarAsync(gradeNome, siglaGrade, matricula);
    }


    // "Excluisao massiva" : planilha CODIGO_GRADE / CODIGO_SKU.
    // Só vincula SKUs a grades que já existem — nunca cria grade nova.
    public async Task<ResultadoOperacao<ImportacaoResultResponse>> ExcluirSkusEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula)
    {
        var (linhas, erroLeitura) = LerArquivo(conteudoArquivo, nomeArquivo, LerPlanilhaExclusao);
        if (erroLeitura is not null)
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida(erroLeitura);

        var erros = new List<ErroLinhaResponse>();
        var itensPorGrade = new Dictionary<int, List<LinhaExclusao>>();

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha.Sku))
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, "CODIGO_SKU é obrigatório."));
                continue;
            }

            if (!int.TryParse(linha.Grade, out var codigoGrade))
            {
                erros.Add(new ErroLinhaResponse(linha.Linha, $"CODIGO_GRADE '{linha.Grade}' inválido — informe o código numérico da grade."));
                continue;
            }

            if (!itensPorGrade.TryGetValue(codigoGrade, out var lista))
            {
                lista = [];
                itensPorGrade[codigoGrade] = lista;
            }

            lista.Add(linha);
        }

        var sucesso = 0;
        foreach (var (codigoGrade, itens) in itensPorGrade)
        {
            var grade = await gradeRepository.ObterPorCodigoAsync(codigoGrade);
            if (grade is null)
            {
                erros.AddRange(itens.Select(i => new ErroLinhaResponse(i.Linha, $"Grade {codigoGrade} não encontrada.")));
                continue;
            }

            var skus = itens.Select(i => i.Sku).Distinct().ToList();
            var vinculados = await gradeRepository.FiltrarSkusVinculadosAsync(codigoGrade, skus);

            erros.AddRange(itens
                .Where(i => !vinculados.Contains(i.Sku))
                .Select(i => new ErroLinhaResponse(i.Linha, $"SKU {i.Sku} não está vinculado à grade {codigoGrade}.")));

            if (vinculados.Count > 0)
            {
                await gradeRepository.DesvincularSkusAsync(codigoGrade, vinculados, matricula);
                sucesso += itens.Count(i => vinculados.Contains(i.Sku));
            }
        }

        return ResultadoOperacao<ImportacaoResultResponse>.ComSucesso(new ImportacaoResultResponse(linhas.Count, sucesso, erros));
    }

    // Validações comuns a todo upload: extensão .xlsx, planilha legível (com
    // as colunas esperadas) e ao menos uma linha de dados.
    private static (List<T> Linhas, string? Erro) LerArquivo<T>(Stream conteudoArquivo, string nomeArquivo, Func<Stream, List<T>> lerPlanilha)
    {
        if (!Path.GetExtension(nomeArquivo).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return ([], "Apenas arquivos .xlsx são aceitos.");

        List<T> linhas;
        try
        {
            linhas = lerPlanilha(conteudoArquivo);
        }
        catch (Exception ex)
        {
            return ([], $"Não foi possível ler a planilha: {ex.Message}");
        }

        return linhas.Count == 0 ? ([], "A planilha não contém dados.") : (linhas, null);
    }

    private static List<LinhaCriacao> LerPlanilhaCriacao(Stream arquivo) =>
        LerPlanilha(arquivo, ["NOME_GRADE", "CODIGO_SKU", "SIGLA"], (linha, valores) => new LinhaCriacao(linha, Sku: valores[1], GradeNome: valores[0], Sigla: valores[2]));

    private static List<LinhaAtualizacao> LerPlanilhaAtualizacao(Stream arquivo) =>
        LerPlanilha(arquivo, ["CODIGO_GRADE", "CODIGO_SKU"], (linha, valores) => new LinhaAtualizacao(linha, valores[0], valores[1]));

    private static List<LinhaExclusao> LerPlanilhaExclusao(Stream arquivo) =>
        LerPlanilha(arquivo, ["CODIGO_GRADE", "CODIGO_SKU"], (linha, valores) => new LinhaExclusao(linha, valores[0], valores[1]));

    // Lê as colunas pedidas (localizadas pelo cabeçalho da linha 1, em qualquer
    // ordem) a partir da linha 2, ignorando linhas totalmente em branco.
    private static List<T> LerPlanilha<T>(Stream arquivo, string[] colunasEsperadas, Func<int, string[], T> criarLinha)
    {
        using var workbook = new XLWorkbook(arquivo);
        var planilha = workbook.Worksheet(1);
        var colunas = MapearColunas(planilha, colunasEsperadas);
        var ultimaLinha = planilha.LastRowUsed()?.RowNumber() ?? 1;

        var linhas = new List<T>();
        for (var linhaAtual = 2; linhaAtual <= ultimaLinha; linhaAtual++)
        {
            var valores = colunasEsperadas
                .Select(coluna => planilha.Cell(linhaAtual, colunas[coluna]).GetString().Trim())
                .ToArray();

            if (valores.All(string.IsNullOrWhiteSpace))
                continue;

            linhas.Add(criarLinha(linhaAtual, valores));
        }

        return linhas;
    }

    private static Dictionary<string, int> MapearColunas(IXLWorksheet planilha, params string[] colunasEsperadas)
    {
        var colunas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var celula in planilha.Row(1).CellsUsed())
        {
            var nome = celula.GetString().Trim();
            if (!string.IsNullOrEmpty(nome))
                colunas[nome] = celula.Address.ColumnNumber;
        }

        var faltantes = colunasEsperadas.Where(c => !colunas.ContainsKey(c)).ToList();
        if (faltantes.Count > 0)
            throw new InvalidOperationException($"Coluna(s) obrigatória(s) não encontrada(s): {string.Join(", ", faltantes)}.");

        return colunas;
    }

    private sealed record LinhaCriacao(int Linha, string Sku, string GradeNome, string Sigla);

    private sealed record LinhaAtualizacao(int Linha, string CodigoGrade, string CodigoSku);

    private sealed record LinhaExclusao(int Linha, string Grade, string Sku);
}
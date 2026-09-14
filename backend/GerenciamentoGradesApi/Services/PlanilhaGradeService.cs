using ClosedXML.Excel;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Repositories.Interfaces;
using GerenciamentoGradesApi.Services.Interfaces;
using GerenciamentoGradesApi.Services.Resultados;

namespace GerenciamentoGradesApi.Services;

public class PlanilhaGradeService : IPlanilhaGradeService
{
    private readonly IGradeRepository _gradeRepository;

    public PlanilhaGradeService(IGradeRepository gradeRepository)
    {
        _gradeRepository = gradeRepository;
    }

    public byte[] GerarModeloImportacaoMassiva()
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add("Grades");

        planilha.Cell(1, 1).Value = "SKU";
        planilha.Cell(1, 2).Value = "NOME_GRADE";
        planilha.Cell(1, 3).Value = "SIGLA";
        planilha.Range(1, 1, 1, 3).Style.Font.Bold = true;
        planilha.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] GerarModeloExclusaoMassivaSkus()
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add("SKUs");

        planilha.Cell(1, 1).Value = "GRADE";
        planilha.Cell(1, 2).Value = "SKU";
        planilha.Range(1, 1, 1, 2).Style.Font.Bold = true;
        planilha.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // "Criação massiva": pode criar grades novas quando o NOME_GRADE da
    // planilha ainda não existe.
    public Task<ResultadoOperacao<ImportacaoResultResponse>> ImportarAsync(Stream conteudoArquivo, string nomeArquivo, string matricula) =>
        ProcessarArquivoAsync(conteudoArquivo, nomeArquivo, matricula, permiteCriarNovaGrade: true);

    // "Importação massiva" (atualização): mesma planilha e mesmas regras da
    // criação, mas só vincula SKUs a grades que já existem — NOME_GRADE
    // inexistente é rejeitado em vez de criar uma grade nova.
    public Task<ResultadoOperacao<ImportacaoResultResponse>> AtualizarEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula) =>
        ProcessarArquivoAsync(conteudoArquivo, nomeArquivo, matricula, permiteCriarNovaGrade: false);

    private async Task<ResultadoOperacao<ImportacaoResultResponse>> ProcessarArquivoAsync(
        Stream conteudoArquivo, string nomeArquivo, string matricula, bool permiteCriarNovaGrade)
    {
        var erroExtensao = ValidarExtensao(nomeArquivo);
        if (erroExtensao is not null)
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida(erroExtensao);

        List<LinhaImportacao> linhas;
        try
        {
            linhas = LerPlanilhaImportacao(conteudoArquivo);
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida($"Não foi possível ler a planilha: {ex.Message}");
        }

        if (linhas.Count == 0)
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida("A planilha não contém dados.");

        var (linhasValidas, errosValidacao) = ValidarLinhasImportacao(linhas);
        var (sucesso, errosProcessamento) = await ProcessarVinculosAsync(linhasValidas, permiteCriarNovaGrade, matricula);

        var resultado = new ImportacaoResultResponse
        {
            TotalLinhas = linhas.Count,
            Sucesso = sucesso,
            Erros = [.. errosValidacao, .. errosProcessamento]
        };

        return ResultadoOperacao<ImportacaoResultResponse>.ComSucesso(resultado);
    }

    private static (List<LinhaImportacao> Validas, List<ErroLinhaResponse> Erros) ValidarLinhasImportacao(List<LinhaImportacao> linhas)
    {
        var erros = new List<ErroLinhaResponse>();
        var validas = new List<LinhaImportacao>();

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha.Sku) || string.IsNullOrWhiteSpace(linha.GradeNome) || string.IsNullOrWhiteSpace(linha.Sigla))
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = "SKU, NOME_GRADE e SIGLA são obrigatórios." });
                continue;
            }

            if (linha.GradeNome.Length > 80)
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = "NOME_GRADE deve ter no máximo 80 caracteres." });
                continue;
            }

            if (linha.Sigla.Length > 15)
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = "SIGLA deve ter no máximo 15 caracteres." });
                continue;
            }

            validas.Add(linha);
        }

        return (validas, erros);
    }

    // Núcleo compartilhado entre criação e atualização em massa: agrupa por
    // NOME_GRADE, resolve/cria o código de cada grade (conforme
    // `permiteCriarNovaGrade`) e aplica a regra "SKU só vincula se estiver
    // livre ou já pertencer à própria grade de destino" — a mesma usada na
    // adição individual de SKUs (GradeService.AdicionarSkusAsync).
    private async Task<(int Sucesso, List<ErroLinhaResponse> Erros)> ProcessarVinculosAsync(
        List<LinhaImportacao> linhasValidas, bool permiteCriarNovaGrade, string matricula)
    {
        var erros = new List<ErroLinhaResponse>();
        var skusExistentes = await _gradeRepository.FiltrarSkusExistentesAsync(linhasValidas.Select(l => l.Sku));

        var itensPorGrade = new Dictionary<string, List<LinhaImportacao>>(StringComparer.OrdinalIgnoreCase);

        foreach (var linha in linhasValidas)
        {
            if (!skusExistentes.Contains(linha.Sku))
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = $"SKU {linha.Sku} não encontrado." });
                continue;
            }

            if (!itensPorGrade.TryGetValue(linha.GradeNome, out var lista))
            {
                lista = [];
                itensPorGrade[linha.GradeNome] = lista;
            }

            lista.Add(linha);
        }

        // Regra de negócio: um SKU só pode ser vinculado se estiver livre ou já
        // pertencer à própria grade de destino da linha — nunca "roubado" de
        // outra grade pela importação. `vinculoEfetivo` começa como o estado
        // atual do banco e é atualizado a cada grupo processado, para também
        // pegar o caso de duas linhas do MESMO arquivo disputando o mesmo SKU
        // para grades diferentes.
        var vinculos = await _gradeRepository.ObterVinculoAtualAsync(skusExistentes);
        var vinculoEfetivo = vinculos.ToDictionary(kv => kv.Key, kv => kv.Value.CodigoGrade, StringComparer.OrdinalIgnoreCase);
        var nomesGradeCache = new Dictionary<int, string?>();

        async Task<string?> ObterNomeGradeAsync(int codigoGrade)
        {
            if (!nomesGradeCache.TryGetValue(codigoGrade, out var nome))
            {
                var grade = await _gradeRepository.ObterPorCodigoAsync(codigoGrade);
                nome = grade?.Nome;
                nomesGradeCache[codigoGrade] = nome;
            }

            return nome;
        }

        var sucesso = 0;
        foreach (var (gradeNome, itens) in itensPorGrade)
        {
            // A mesma GRADE (nome) não pode aparecer com siglas diferentes dentro
            // do próprio arquivo — sem isso, qual das duas seria usada ficaria
            // arbitrário (a da primeira linha lida).
            var siglasDaGrade = itens.Select(i => i.Sigla).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (siglasDaGrade.Count > 1)
            {
                erros.AddRange(itens.Select(i => new ErroLinhaResponse
                {
                    Linha = i.Linha,
                    Mensagem = $"NOME_GRADE '{gradeNome}' aparece com siglas diferentes na planilha ({string.Join(", ", siglasDaGrade)})."
                }));
                continue;
            }

            var codigoGrade = await ResolverCodigoGradeAsync(gradeNome, siglasDaGrade[0], permiteCriarNovaGrade, matricula, itens, erros);
            if (codigoGrade is null)
                continue;

            var bloqueados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var paraVincular = new List<string>();

            foreach (var sku in itens.Select(i => i.Sku).Distinct())
            {
                vinculoEfetivo.TryGetValue(sku, out var codigoAtual);

                if (codigoAtual is int codigo && codigo != codigoGrade.Value)
                    bloqueados.Add(sku);
                else if (codigoAtual != codigoGrade.Value)
                    paraVincular.Add(sku);
            }

            if (paraVincular.Count > 0)
            {
                await _gradeRepository.VincularSkusAsync(codigoGrade.Value, paraVincular, matricula);
                foreach (var sku in paraVincular)
                    vinculoEfetivo[sku] = codigoGrade.Value;
            }

            foreach (var linha in itens)
            {
                if (!bloqueados.Contains(linha.Sku))
                {
                    sucesso++;
                    continue;
                }

                var codigoBloqueio = vinculoEfetivo.GetValueOrDefault(linha.Sku);
                var nomeBloqueio = codigoBloqueio is int cb ? await ObterNomeGradeAsync(cb) : null;

                erros.Add(new ErroLinhaResponse
                {
                    Linha = linha.Linha,
                    Mensagem = $"SKU {linha.Sku} já está vinculado à grade {codigoBloqueio} - {nomeBloqueio}."
                });
            }
        }

        return (sucesso, erros);
    }

    // Resolve o código da grade de destino para um grupo de linhas, ou
    // registra o(s) erro(s) e devolve null quando o grupo inteiro deve ser
    // rejeitado (sigla divergente, sigla já usada por outra grade, ou — na
    // atualização — grade inexistente).
    private async Task<int?> ResolverCodigoGradeAsync(
        string gradeNome, string siglaGrade, bool permiteCriarNovaGrade, string matricula,
        List<LinhaImportacao> itens, List<ErroLinhaResponse> erros)
    {
        var gradeExistente = await _gradeRepository.ObterPorNomeAsync(gradeNome);

        if (gradeExistente is not null)
        {
            // A grade já existe — a sigla da planilha não é usada pra
            // alterá-la silenciosamente; se divergir da sigla real, é erro.
            if (!string.Equals(gradeExistente.Sigla, siglaGrade, StringComparison.OrdinalIgnoreCase))
            {
                erros.AddRange(itens.Select(i => new ErroLinhaResponse
                {
                    Linha = i.Linha,
                    Mensagem = $"NOME_GRADE '{gradeNome}' já existe com a sigla '{gradeExistente.Sigla}' — a sigla informada '{siglaGrade}' diverge."
                }));
                return null;
            }

            return gradeExistente.Codigo;
        }

        if (!permiteCriarNovaGrade)
        {
            erros.AddRange(itens.Select(i => new ErroLinhaResponse
            {
                Linha = i.Linha,
                Mensagem = $"NOME_GRADE '{gradeNome}' não encontrada — esta importação só atualiza SKUs de grades já existentes."
            }));
            return null;
        }

        // Grade nova — a sigla não pode colidir com a de uma grade diferente
        // já existente (mesma regra aplicada na criação individual, ver
        // GradeService.CriarAsync).
        var conflito = await _gradeRepository.ObterPorNomeOuSiglaAsync(gradeNome, siglaGrade);
        if (conflito is not null)
        {
            erros.AddRange(itens.Select(i => new ErroLinhaResponse
            {
                Linha = i.Linha,
                Mensagem = $"SIGLA '{siglaGrade}' já está em uso pela grade {conflito.Codigo} - {conflito.Nome}."
            }));
            return null;
        }

        return await _gradeRepository.CriarAsync(gradeNome, siglaGrade, matricula);
    }

    public async Task<ResultadoOperacao<ImportacaoResultResponse>> ExcluirSkusEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula)
    {
        var erroExtensao = ValidarExtensao(nomeArquivo);
        if (erroExtensao is not null)
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida(erroExtensao);

        List<LinhaExclusao> linhas;
        try
        {
            linhas = LerPlanilhaExclusao(conteudoArquivo);
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida($"Não foi possível ler a planilha: {ex.Message}");
        }

        if (linhas.Count == 0)
            return ResultadoOperacao<ImportacaoResultResponse>.EntradaInvalida("A planilha não contém dados.");

        var erros = new List<ErroLinhaResponse>();
        var itensPorGrade = new Dictionary<int, List<LinhaExclusao>>();

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha.Sku))
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = "SKU é obrigatório." });
                continue;
            }

            if (!int.TryParse(linha.Grade, out var codigoGrade))
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = $"GRADE '{linha.Grade}' inválida — informe o código numérico da grade." });
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
            var grade = await _gradeRepository.ObterPorCodigoAsync(codigoGrade);
            if (grade is null)
            {
                erros.AddRange(itens.Select(i => new ErroLinhaResponse { Linha = i.Linha, Mensagem = $"Grade {codigoGrade} não encontrada." }));
                continue;
            }

            var skus = itens.Select(i => i.Sku).Distinct().ToList();
            var vinculados = await _gradeRepository.FiltrarSkusVinculadosAsync(codigoGrade, skus);

            erros.AddRange(itens
                .Where(i => !vinculados.Contains(i.Sku))
                .Select(i => new ErroLinhaResponse { Linha = i.Linha, Mensagem = $"SKU {i.Sku} não está vinculado à grade {codigoGrade}." }));

            if (vinculados.Count > 0)
            {
                await _gradeRepository.DesvincularSkusAsync(codigoGrade, vinculados, matricula);
                sucesso += itens.Count(i => vinculados.Contains(i.Sku));
            }
        }

        var resultado = new ImportacaoResultResponse
        {
            TotalLinhas = linhas.Count,
            Sucesso = sucesso,
            Erros = erros
        };

        return ResultadoOperacao<ImportacaoResultResponse>.ComSucesso(resultado);
    }

    private static string? ValidarExtensao(string nomeArquivo)
    {
        if (!Path.GetExtension(nomeArquivo).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return "Apenas arquivos .xlsx são aceitos.";

        return null;
    }

    private static List<LinhaImportacao> LerPlanilhaImportacao(Stream arquivo)
    {
        using var workbook = new XLWorkbook(arquivo);
        var planilha = workbook.Worksheet(1);
        var colunas = MapearColunas(planilha, "SKU", "NOME_GRADE", "SIGLA");
        var ultimaLinha = planilha.LastRowUsed()?.RowNumber() ?? 1;

        var linhas = new List<LinhaImportacao>();
        for (var linhaAtual = 2; linhaAtual <= ultimaLinha; linhaAtual++)
        {
            var sku = planilha.Cell(linhaAtual, colunas["SKU"]).GetString().Trim();
            var grade = planilha.Cell(linhaAtual, colunas["NOME_GRADE"]).GetString().Trim();
            var sigla = planilha.Cell(linhaAtual, colunas["SIGLA"]).GetString().Trim();

            if (string.IsNullOrWhiteSpace(sku) && string.IsNullOrWhiteSpace(grade) && string.IsNullOrWhiteSpace(sigla))
                continue;

            linhas.Add(new LinhaImportacao(linhaAtual, sku, grade, sigla));
        }

        return linhas;
    }

    private static List<LinhaExclusao> LerPlanilhaExclusao(Stream arquivo)
    {
        using var workbook = new XLWorkbook(arquivo);
        var planilha = workbook.Worksheet(1);
        var colunas = MapearColunas(planilha, "GRADE", "SKU");
        var ultimaLinha = planilha.LastRowUsed()?.RowNumber() ?? 1;

        var linhas = new List<LinhaExclusao>();
        for (var linhaAtual = 2; linhaAtual <= ultimaLinha; linhaAtual++)
        {
            var grade = planilha.Cell(linhaAtual, colunas["GRADE"]).GetString().Trim();
            var sku = planilha.Cell(linhaAtual, colunas["SKU"]).GetString().Trim();

            if (string.IsNullOrWhiteSpace(grade) && string.IsNullOrWhiteSpace(sku))
                continue;

            linhas.Add(new LinhaExclusao(linhaAtual, grade, sku));
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

    private sealed record LinhaImportacao(int Linha, string Sku, string GradeNome, string Sigla);

    private sealed record LinhaExclusao(int Linha, string Grade, string Sku);
}

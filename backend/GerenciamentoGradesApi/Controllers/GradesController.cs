using ClosedXML.Excel;
using GerenciamentoGradesApi.Dtos.Requests;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Middleware;
using GerenciamentoGradesApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GerenciamentoGradesApi.Controllers;

[ApiController]
[Route("api/grades")]
public class GradesController : ControllerBase
{
    // 2627 = violação de PK/índice único; 2601 = violação de índice único não-clusterizado.
    private const int SqlErroViolacaoUnicidade = 2627;
    private const int SqlErroIndiceUnico = 2601;

    private readonly IGradeRepository _gradeRepository;

    public GradesController(IGradeRepository gradeRepository)
    {
        _gradeRepository = gradeRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GradeListItemResponse>>> Listar([FromQuery] int? codigo, [FromQuery] string? nome)
    {
        var grades = await _gradeRepository.ListarAsync(codigo, nome);

        var resposta = grades.Select(g => new GradeListItemResponse
        {
            Codigo = g.Codigo,
            Nome = g.Nome,
            Sigla = g.Sigla,
            QtdSkus = g.QtdSkus
        });

        return Ok(resposta);
    }

    [HttpGet("{codigo:int}")]
    public async Task<ActionResult<GradeDetalheResponse>> ObterDetalhe(int codigo)
    {
        var detalhe = await MontarDetalheAsync(codigo);
        if (detalhe is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        return Ok(detalhe);
    }

    [HttpGet("{codigo:int}/skus-disponiveis")]
    public async Task<ActionResult<IEnumerable<SkuResumoResponse>>> BuscarSkusDisponiveis(int codigo, [FromQuery] string? termo)
    {
        var grade = await _gradeRepository.ObterPorCodigoAsync(codigo);
        if (grade is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        var skus = await _gradeRepository.BuscarSkusDisponiveisAsync(termo?.Trim() ?? string.Empty, codigo);

        return Ok(skus.Select(s => new SkuResumoResponse { Codigo = s.Codigo, Descricao = s.Descricao }));
    }

    [HttpPost("{codigo:int}/skus")]
    public async Task<ActionResult<AtualizarSkusResponse>> AdicionarSkus(int codigo, [FromBody] SkusRequest request)
    {
        var grade = await _gradeRepository.ObterPorCodigoAsync(codigo);
        if (grade is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        var skus = request.Skus.Distinct().ToList();
        var existentes = await _gradeRepository.FiltrarSkusExistentesAsync(skus);

        var rejeitados = skus
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
            await _gradeRepository.VincularSkusAsync(codigo, paraVincular, HttpContext.ObterMatricula());

        var detalhe = await MontarDetalheAsync(codigo);
        return Ok(new AtualizarSkusResponse { Grade = detalhe!, SkusRejeitados = rejeitados });
    }

    [HttpPost("{codigo:int}/skus/remover")]
    public async Task<ActionResult<AtualizarSkusResponse>> RemoverSkus(int codigo, [FromBody] SkusRequest request)
    {
        var grade = await _gradeRepository.ObterPorCodigoAsync(codigo);
        if (grade is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        var skus = request.Skus.Distinct().ToList();
        var vinculados = await _gradeRepository.FiltrarSkusVinculadosAsync(codigo, skus);

        var rejeitados = skus
            .Where(s => !vinculados.Contains(s))
            .Select(s => new SkuRejeitadoResponse { Sku = s, Mensagem = $"SKU {s} não está vinculado a esta grade." })
            .ToList();

        if (vinculados.Count > 0)
            await _gradeRepository.DesvincularSkusAsync(codigo, vinculados, HttpContext.ObterMatricula());

        var detalhe = await MontarDetalheAsync(codigo);
        return Ok(new AtualizarSkusResponse { Grade = detalhe!, SkusRejeitados = rejeitados });
    }

    private async Task<GradeDetalheResponse?> MontarDetalheAsync(int codigo)
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
            Skus = skus.Select(s => new SkuResumoResponse { Codigo = s.Codigo, Descricao = s.Descricao }).ToList()
        };
    }

    [HttpPost]
    public async Task<ActionResult<GradeResponse>> Criar([FromBody] CriarGradeRequest request)
    {
        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim();

        try
        {
            var codigo = await _gradeRepository.CriarAsync(nome, sigla, HttpContext.ObterMatricula());
            var resposta = new GradeResponse { Codigo = codigo, Nome = nome, Sigla = sigla };

            return CreatedAtAction(nameof(ObterDetalhe), new { codigo }, resposta);
        }
        catch (SqlException ex) when (ex.Number is SqlErroViolacaoUnicidade or SqlErroIndiceUnico)
        {
            return Conflict(new { mensagem = "Já existe uma grade com esse nome ou sigla." });
        }
    }

    [HttpPut("{codigo:int}")]
    public async Task<ActionResult<GradeResponse>> Atualizar(int codigo, [FromBody] AtualizarGradeRequest request)
    {
        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim();

        try
        {
            var atualizado = await _gradeRepository.AtualizarAsync(codigo, nome, sigla, HttpContext.ObterMatricula());
            if (!atualizado)
                return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

            return Ok(new GradeResponse { Codigo = codigo, Nome = nome, Sigla = sigla });
        }
        catch (SqlException ex) when (ex.Number is SqlErroViolacaoUnicidade or SqlErroIndiceUnico)
        {
            return Conflict(new { mensagem = "Já existe uma grade com esse nome ou sigla." });
        }
    }

    [HttpDelete("{codigo:int}")]
    public async Task<IActionResult> Excluir(int codigo)
    {
        var excluido = await _gradeRepository.ExcluirAsync(codigo, HttpContext.ObterMatricula());
        if (!excluido)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        return NoContent();
    }

    [HttpGet("importacao-massiva/modelo")]
    public IActionResult BaixarModeloImportacaoMassiva()
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add("Grades");

        planilha.Cell(1, 1).Value = "SKU";
        planilha.Cell(1, 2).Value = "GRADE";
        planilha.Cell(1, 3).Value = "SIGLA";
        planilha.Range(1, 1, 1, 3).Style.Font.Bold = true;
        planilha.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "modelo_criacao_massiva_grades.xlsx");
    }

    [HttpGet("exclusao-massiva-skus/modelo")]
    public IActionResult BaixarModeloExclusaoMassivaSkus()
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add("SKUs");

        planilha.Cell(1, 1).Value = "GRADE";
        planilha.Cell(1, 2).Value = "SKU";
        planilha.Range(1, 1, 1, 2).Style.Font.Bold = true;
        planilha.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "modelo_exclusao_massiva_skus.xlsx");
    }

    [HttpPost("importacao-massiva")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ImportacaoResultResponse>> ImportacaoMassiva(IFormFile file)
    {
        var validacaoArquivo = ValidarArquivo(file);
        if (validacaoArquivo is not null)
            return validacaoArquivo;

        List<LinhaImportacao> linhas;
        try
        {
            linhas = LerPlanilhaImportacao(file);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = $"Não foi possível ler a planilha: {ex.Message}" });
        }

        if (linhas.Count == 0)
            return BadRequest(new { mensagem = "A planilha não contém dados." });

        var matricula = HttpContext.ObterMatricula();
        var erros = new List<ErroLinhaResponse>();
        var linhasValidas = new List<LinhaImportacao>();

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha.Sku) || string.IsNullOrWhiteSpace(linha.GradeNome) || string.IsNullOrWhiteSpace(linha.Sigla))
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = "SKU, GRADE e SIGLA são obrigatórios." });
                continue;
            }

            if (linha.GradeNome.Length > 80)
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = "GRADE deve ter no máximo 80 caracteres." });
                continue;
            }

            if (linha.Sigla.Length > 15)
            {
                erros.Add(new ErroLinhaResponse { Linha = linha.Linha, Mensagem = "SIGLA deve ter no máximo 15 caracteres." });
                continue;
            }

            linhasValidas.Add(linha);
        }

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
            var codigoGrade = await _gradeRepository.ObterCodigoPorNomeAsync(gradeNome)
                ?? await _gradeRepository.CriarAsync(gradeNome, itens[0].Sigla, matricula);

            var bloqueados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var paraVincular = new List<string>();

            foreach (var sku in itens.Select(i => i.Sku).Distinct())
            {
                vinculoEfetivo.TryGetValue(sku, out var codigoAtual);

                if (codigoAtual is int codigo && codigo != codigoGrade)
                    bloqueados.Add(sku);
                else if (codigoAtual != codigoGrade)
                    paraVincular.Add(sku);
            }

            if (paraVincular.Count > 0)
            {
                await _gradeRepository.VincularSkusAsync(codigoGrade, paraVincular, matricula);
                foreach (var sku in paraVincular)
                    vinculoEfetivo[sku] = codigoGrade;
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

        return Ok(new ImportacaoResultResponse
        {
            TotalLinhas = linhas.Count,
            Sucesso = sucesso,
            Erros = erros
        });
    }

    [HttpPost("exclusao-massiva-skus")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ImportacaoResultResponse>> ExclusaoMassivaSkus(IFormFile file)
    {
        var validacaoArquivo = ValidarArquivo(file);
        if (validacaoArquivo is not null)
            return validacaoArquivo;

        List<LinhaExclusao> linhas;
        try
        {
            linhas = LerPlanilhaExclusao(file);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = $"Não foi possível ler a planilha: {ex.Message}" });
        }

        if (linhas.Count == 0)
            return BadRequest(new { mensagem = "A planilha não contém dados." });

        var matricula = HttpContext.ObterMatricula();
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

        return Ok(new ImportacaoResultResponse
        {
            TotalLinhas = linhas.Count,
            Sucesso = sucesso,
            Erros = erros
        });
    }

    private static BadRequestObjectResult? ValidarArquivo(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return new BadRequestObjectResult(new { mensagem = "Nenhum arquivo enviado." });

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return new BadRequestObjectResult(new { mensagem = "Apenas arquivos .xlsx são aceitos." });

        return null;
    }

    private static List<LinhaImportacao> LerPlanilhaImportacao(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var planilha = workbook.Worksheet(1);
        var colunas = MapearColunas(planilha, "SKU", "GRADE", "SIGLA");
        var ultimaLinha = planilha.LastRowUsed()?.RowNumber() ?? 1;

        var linhas = new List<LinhaImportacao>();
        for (var linhaAtual = 2; linhaAtual <= ultimaLinha; linhaAtual++)
        {
            var sku = planilha.Cell(linhaAtual, colunas["SKU"]).GetString().Trim();
            var grade = planilha.Cell(linhaAtual, colunas["GRADE"]).GetString().Trim();
            var sigla = planilha.Cell(linhaAtual, colunas["SIGLA"]).GetString().Trim();

            if (string.IsNullOrWhiteSpace(sku) && string.IsNullOrWhiteSpace(grade) && string.IsNullOrWhiteSpace(sigla))
                continue;

            linhas.Add(new LinhaImportacao(linhaAtual, sku, grade, sigla));
        }

        return linhas;
    }

    private static List<LinhaExclusao> LerPlanilhaExclusao(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
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

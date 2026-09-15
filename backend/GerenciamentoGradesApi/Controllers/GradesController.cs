using GerenciamentoGradesApi.Dtos.Requests;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Middleware;
using GerenciamentoGradesApi.Services.Interfaces;
using GerenciamentoGradesApi.Services.Resultados;
using Microsoft.AspNetCore.Mvc;

namespace GerenciamentoGradesApi.Controllers;

// Só orquestra HTTP: recebe a requisição, chama o serviço e traduz o
// resultado em IActionResult. Nenhuma regra de negócio deve morar aqui —
// isso vive em Services/GradeService, Services/PlanilhaGradeService e
// Services/ExportacaoDiagnosticoService.
[ApiController]
[Route("api/grades")]
public class GradesController(
    IGradeService gradeService,
    IPlanilhaGradeService planilhaGradeService,
    IExportacaoDiagnosticoService exportacaoDiagnosticoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GradeListItemResponse>>> Listar([FromQuery] int? codigoGrade, [FromQuery] string? nome)
    {
        var grades = await gradeService.ListarAsync(codigoGrade, nome);
        return Ok(grades);
    }

    [HttpGet("{codigoGrade:int}")]
    public async Task<ActionResult<GradeDetalheResponse>> ObterDetalhe(int codigoGrade)
    {
        var detalhe = await gradeService.ObterDetalheAsync(codigoGrade);
        if (detalhe is null)
            return NotFound(new { mensagem = $"Grade {codigoGrade} não encontrada." });

        return Ok(detalhe);
    }

    [HttpGet("skus-orfaos")]
    public async Task<ActionResult<SkusOrfaosResponse>> ListarSkusOrfaos(
        [FromQuery] string? termo, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
    {
        if (pagina < 1) pagina = 1;
        if (tamanhoPagina is < 1 or > 100) tamanhoPagina = 20;

        var resultado = await gradeService.ListarSkusOrfaosAsync(termo, pagina, tamanhoPagina);
        return Ok(resultado);
    }

    [HttpGet("skus-orfaos/exportar")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK, "text/csv")]
    public IResult ExportarSkusOrfaosCsv([FromQuery] string? termo)
    {
        var nomeArquivo = $"skus_orfaos_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        // Results.Stream escreve no corpo da resposta à medida que as linhas
        // saem do banco — o arquivo nunca é montado inteiro em memória (a lista
        // de órfãos pode ter centenas de milhares de produtos).
        return Results.Stream(
            destino => exportacaoDiagnosticoService.EscreverSkusOrfaosCsvAsync(destino, termo, HttpContext.RequestAborted),
            contentType: "text/csv; charset=utf-8",
            fileDownloadName: nomeArquivo);
    }

    [HttpGet("grades-vazias")]
    public async Task<ActionResult<IEnumerable<GradeListItemResponse>>> ListarGradesVazias(
        [FromQuery] int? codigoGrade, [FromQuery] string? nome)
    {
        var grades = await gradeService.ListarGradesVaziasAsync(codigoGrade, nome);
        return Ok(grades);
    }

    [HttpGet("{codigoGrade:int}/skus-disponiveis")]
    public async Task<ActionResult<IEnumerable<SkuResumoResponse>>> BuscarSkusDisponiveis(int codigoGrade, [FromQuery] string? termo)
    {
        var skus = await gradeService.BuscarSkusDisponiveisAsync(codigoGrade, termo);
        if (skus is null)
            return NotFound(new { mensagem = $"Grade {codigoGrade} não encontrada." });

        return Ok(skus);
    }

    [HttpPost("{codigoGrade:int}/skus")]
    public async Task<ActionResult<AtualizarSkusResponse>> AdicionarSkus(int codigoGrade, [FromBody] SkusRequest request)
    {
        var resultado = await gradeService.AdicionarSkusAsync(codigoGrade, request.Skus, HttpContext.ObterMatricula());
        if (resultado is null)
            return NotFound(new { mensagem = $"Grade {codigoGrade} não encontrada." });

        return Ok(resultado);
    }

    [HttpPost("{codigoGrade:int}/skus/remover")]
    public async Task<ActionResult<AtualizarSkusResponse>> RemoverSkus(int codigoGrade, [FromBody] SkusRequest request)
    {
        var resultado = await gradeService.RemoverSkusAsync(codigoGrade, request.Skus, HttpContext.ObterMatricula());
        if (resultado is null)
            return NotFound(new { mensagem = $"Grade {codigoGrade} não encontrada." });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<GradeResponse>> Criar([FromBody] CriarGradeRequest request)
    {
        var resultado = await gradeService.CriarAsync(request, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => CreatedAtAction(nameof(ObterDetalhe), new { codigoGrade = resultado.Valor!.CodigoGrade }, resultado.Valor),
            StatusOperacao.Conflito => Conflict(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }

    [HttpPut("{codigoGrade:int}")]
    public async Task<ActionResult<GradeResponse>> Atualizar(int codigoGrade, [FromBody] AtualizarGradeRequest request)
    {
        var resultado = await gradeService.AtualizarAsync(codigoGrade, request, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => Ok(resultado.Valor),
            StatusOperacao.NaoEncontrado => NotFound(new { mensagem = resultado.MensagemErro }),
            StatusOperacao.Conflito => Conflict(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }

    [HttpDelete("{codigoGrade:int}")]
    public async Task<IActionResult> Excluir(int codigoGrade)
    {
        var excluido = await gradeService.ExcluirAsync(codigoGrade, HttpContext.ObterMatricula());
        if (!excluido)
            return NotFound(new { mensagem = $"Grade {codigoGrade} não encontrada." });

        return NoContent();
    }

    [HttpGet("criacao-massiva/modelo")]
    public IActionResult BaixarModeloCriacaoMassiva()
    {
        var arquivo = planilhaGradeService.GerarModeloCriacaoMassiva();
        return File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modelo_criacao_massiva_grades.xlsx");
    }

    [HttpGet("importacao-massiva-atualizacao/modelo")]
    public IActionResult BaixarModeloImportacaoMassivaAtualizacao()
    {
        var arquivo = planilhaGradeService.GerarModeloImportacaoMassivaAtualizacao();
        return File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modelo_importacao_massiva_atualizacao.xlsx");
    }

    [HttpGet("exclusao-massiva-skus/modelo")]
    public IActionResult BaixarModeloExclusaoMassivaSkus()
    {
        var arquivo = planilhaGradeService.GerarModeloExclusaoMassivaSkus();
        return File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modelo_exclusao_massiva_skus.xlsx");
    }

    [HttpPost("criacao-massiva")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ImportacaoResultResponse>> CriacaoMassiva(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { mensagem = "Nenhum arquivo enviado." });

        var resultado = await planilhaGradeService.CriarEmMassaAsync(file.OpenReadStream(), file.FileName, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => Ok(resultado.Valor),
            StatusOperacao.EntradaInvalida => BadRequest(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }

    [HttpPost("importacao-massiva-atualizacao")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ImportacaoResultResponse>> ImportacaoMassivaAtualizacao(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { mensagem = "Nenhum arquivo enviado." });

        var resultado = await planilhaGradeService.AtualizarEmMassaAsync(file.OpenReadStream(), file.FileName, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => Ok(resultado.Valor),
            StatusOperacao.EntradaInvalida => BadRequest(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }

    [HttpPost("exclusao-massiva-skus")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ImportacaoResultResponse>> ExclusaoMassivaSkus(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { mensagem = "Nenhum arquivo enviado." });

        var resultado = await planilhaGradeService.ExcluirSkusEmMassaAsync(file.OpenReadStream(), file.FileName, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => Ok(resultado.Valor),
            StatusOperacao.EntradaInvalida => BadRequest(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }
}

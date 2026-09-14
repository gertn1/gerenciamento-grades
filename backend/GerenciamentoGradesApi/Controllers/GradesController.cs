using GerenciamentoGradesApi.Dtos.Requests;
using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Middleware;
using GerenciamentoGradesApi.Services.Interfaces;
using GerenciamentoGradesApi.Services.Resultados;
using Microsoft.AspNetCore.Mvc;

namespace GerenciamentoGradesApi.Controllers;

// Só orquestra HTTP: recebe a requisição, chama o serviço e traduz o
// resultado em IActionResult. Nenhuma regra de negócio deve morar aqui —
// isso vive em Services/GradeService e Services/PlanilhaGradeService.
[ApiController]
[Route("api/grades")]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;
    private readonly IPlanilhaGradeService _planilhaGradeService;

    public GradesController(IGradeService gradeService, IPlanilhaGradeService planilhaGradeService)
    {
        _gradeService = gradeService;
        _planilhaGradeService = planilhaGradeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GradeListItemResponse>>> Listar([FromQuery] int? codigo, [FromQuery] string? nome)
    {
        var grades = await _gradeService.ListarAsync(codigo, nome);
        return Ok(grades);
    }

    [HttpGet("{codigo:int}")]
    public async Task<ActionResult<GradeDetalheResponse>> ObterDetalhe(int codigo)
    {
        var detalhe = await _gradeService.ObterDetalheAsync(codigo);
        if (detalhe is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        return Ok(detalhe);
    }

    [HttpGet("skus-orfaos")]
    public async Task<ActionResult<SkusOrfaosResponse>> ListarSkusOrfaos(
        [FromQuery] string? termo, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
    {
        if (pagina < 1) pagina = 1;
        if (tamanhoPagina is < 1 or > 100) tamanhoPagina = 20;

        var resultado = await _gradeService.ListarSkusOrfaosAsync(termo, pagina, tamanhoPagina);
        return Ok(resultado);
    }

    [HttpGet("grades-vazias")]
    public async Task<ActionResult<IEnumerable<GradeListItemResponse>>> ListarGradesVazias(
        [FromQuery] int? codigo, [FromQuery] string? nome)
    {
        var grades = await _gradeService.ListarGradesVaziasAsync(codigo, nome);
        return Ok(grades);
    }

    [HttpGet("{codigo:int}/skus-disponiveis")]
    public async Task<ActionResult<IEnumerable<SkuResumoResponse>>> BuscarSkusDisponiveis(int codigo, [FromQuery] string? termo)
    {
        var skus = await _gradeService.BuscarSkusDisponiveisAsync(codigo, termo);
        if (skus is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        return Ok(skus);
    }

    [HttpPost("{codigo:int}/skus")]
    public async Task<ActionResult<AtualizarSkusResponse>> AdicionarSkus(int codigo, [FromBody] SkusRequest request)
    {
        var resultado = await _gradeService.AdicionarSkusAsync(codigo, request.Skus, HttpContext.ObterMatricula());
        if (resultado is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        return Ok(resultado);
    }

    [HttpPost("{codigo:int}/skus/remover")]
    public async Task<ActionResult<AtualizarSkusResponse>> RemoverSkus(int codigo, [FromBody] SkusRequest request)
    {
        var resultado = await _gradeService.RemoverSkusAsync(codigo, request.Skus, HttpContext.ObterMatricula());
        if (resultado is null)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<GradeResponse>> Criar([FromBody] CriarGradeRequest request)
    {
        var resultado = await _gradeService.CriarAsync(request, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => CreatedAtAction(nameof(ObterDetalhe), new { codigo = resultado.Valor!.Codigo }, resultado.Valor),
            StatusOperacao.Conflito => Conflict(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }

    [HttpPut("{codigo:int}")]
    public async Task<ActionResult<GradeResponse>> Atualizar(int codigo, [FromBody] AtualizarGradeRequest request)
    {
        var resultado = await _gradeService.AtualizarAsync(codigo, request, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => Ok(resultado.Valor),
            StatusOperacao.NaoEncontrado => NotFound(new { mensagem = resultado.MensagemErro }),
            StatusOperacao.Conflito => Conflict(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }

    [HttpDelete("{codigo:int}")]
    public async Task<IActionResult> Excluir(int codigo)
    {
        var excluido = await _gradeService.ExcluirAsync(codigo, HttpContext.ObterMatricula());
        if (!excluido)
            return NotFound(new { mensagem = $"Grade {codigo} não encontrada." });

        return NoContent();
    }

    [HttpGet("importacao-massiva/modelo")]
    public IActionResult BaixarModeloImportacaoMassiva()
    {
        var arquivo = _planilhaGradeService.GerarModeloImportacaoMassiva();
        return File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modelo_criacao_massiva_grades.xlsx");
    }

    [HttpGet("exclusao-massiva-skus/modelo")]
    public IActionResult BaixarModeloExclusaoMassivaSkus()
    {
        var arquivo = _planilhaGradeService.GerarModeloExclusaoMassivaSkus();
        return File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modelo_exclusao_massiva_skus.xlsx");
    }

    [HttpPost("importacao-massiva")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ImportacaoResultResponse>> ImportacaoMassiva(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { mensagem = "Nenhum arquivo enviado." });

        var resultado = await _planilhaGradeService.ImportarAsync(file.OpenReadStream(), file.FileName, HttpContext.ObterMatricula());

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

        var resultado = await _planilhaGradeService.AtualizarEmMassaAsync(file.OpenReadStream(), file.FileName, HttpContext.ObterMatricula());

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

        var resultado = await _planilhaGradeService.ExcluirSkusEmMassaAsync(file.OpenReadStream(), file.FileName, HttpContext.ObterMatricula());

        return resultado.Status switch
        {
            StatusOperacao.Sucesso => Ok(resultado.Valor),
            StatusOperacao.EntradaInvalida => BadRequest(new { mensagem = resultado.MensagemErro }),
            _ => Problem(resultado.MensagemErro)
        };
    }
}

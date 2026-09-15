using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Services.Resultados;

namespace GerenciamentoGradesApi.Services.Interfaces;

// Responsabilidade isolada da grade/SKU "pura": geração e leitura das
// planilhas .xlsx usadas na criação, atualização e exclusão em massa. Depende
// só de Stream/string — nada de IFormFile — para não amarrar o serviço a tipos
// do ASP.NET Core (DIP).
public interface IPlanilhaGradeService
{
    // Colunas SKU, NOME_GRADE e SIGLA.
    byte[] GerarModeloCriacaoMassiva();

    // Colunas CODIGO_GRADE e CODIGO_SKU.
    byte[] GerarModeloImportacaoMassivaAtualizacao();

    byte[] GerarModeloExclusaoMassivaSkus();

    Task<ResultadoOperacao<ImportacaoResultResponse>> CriarEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula);

    // Lê a planilha CODIGO_GRADE / CODIGO_SKU e nunca cria grade nova — o
    // CODIGO_GRADE precisa já existir, senão a linha é rejeitada.
    Task<ResultadoOperacao<ImportacaoResultResponse>> AtualizarEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula);

    Task<ResultadoOperacao<ImportacaoResultResponse>> ExcluirSkusEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula);
}

using GerenciamentoGradesApi.Dtos.Responses;
using GerenciamentoGradesApi.Services.Resultados;

namespace GerenciamentoGradesApi.Services.Interfaces;

// Responsabilidade isolada da grade/SKU "pura": geração e leitura das
// planilhas .xlsx usadas na criação e exclusão em massa. Depende só de
// Stream/string — nada de IFormFile — para não amarrar o serviço a tipos do
// ASP.NET Core (DIP).
public interface IPlanilhaGradeService
{
    byte[] GerarModeloImportacaoMassiva();

    byte[] GerarModeloExclusaoMassivaSkus();

    Task<ResultadoOperacao<ImportacaoResultResponse>> ImportarAsync(Stream conteudoArquivo, string nomeArquivo, string matricula);

    // Mesma planilha e mesmas regras da criação em massa, mas nunca cria
    // grade nova — NOME_GRADE precisa já existir, senão a linha é rejeitada.
    Task<ResultadoOperacao<ImportacaoResultResponse>> AtualizarEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula);

    Task<ResultadoOperacao<ImportacaoResultResponse>> ExcluirSkusEmMassaAsync(Stream conteudoArquivo, string nomeArquivo, string matricula);
}

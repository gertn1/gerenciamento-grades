namespace GerenciamentoGradesApi.Services.Interfaces;

// Exportações da tela de Diagnóstico. Escreve direto num Stream (o corpo da
// resposta HTTP, no caso da API) em vez de devolver byte[] — a lista de SKUs
// órfãos pode ser grande demais para ser montada inteira em memória.
public interface IExportacaoDiagnosticoService
{
    // CSV (UTF-8 com BOM, separador ";") com todos os SKUs sem grade vinculada,
    // opcionalmente filtrados pelo prefixo do código do SKU (`termo`).
    Task EscreverSkusOrfaosCsvAsync(Stream destino, string? termo, CancellationToken cancellationToken = default);
}

using System.Globalization;
using System.Text;
using GerenciamentoGradesApi.Repositories.Interfaces;
using GerenciamentoGradesApi.Services.Interfaces;

namespace GerenciamentoGradesApi.Services;

public class ExportacaoDiagnosticoService(IGradeRepository gradeRepository) : IExportacaoDiagnosticoService
{
    // ";" é o separador de lista do Excel em pt-BR — com "," o arquivo abriria
    // com tudo numa coluna só.
    private const char Separador = ';';
    private const string StatusSkuOrfao = "SEM_GRADE_VINCULADA";

    private static readonly string[] Cabecalho = ["CODIGO_SKU", "DESCRICAO", "STATUS", "DATA_EXPORTACAO"];
    private static readonly char[] CaracteresQueExigemAspas = [Separador, '"', '\r', '\n'];

    // Com BOM o Excel reconhece o arquivo como UTF-8; sem ele, os acentos das
    // descrições dos produtos aparecem corrompidos.
    private static readonly Encoding Utf8ComBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public async Task EscreverSkusOrfaosCsvAsync(Stream destino, string? termo, CancellationToken cancellationToken = default)
    {
        // PRODUTO_MESTRE não tem data de desvinculação mapeada; a data do
        // arquivo é a da extração, igual em todas as linhas.
        var dataExportacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        // O buffer do StreamWriter é fixo: quando enche, WriteLineAsync descarrega
        // no destino — o consumo de memória não cresce com o número de linhas.
        // leaveOpen: o stream de destino pertence a quem chamou.
        // NewLine fixo em CRLF (RFC 4180), independente do SO do servidor.
        await using var escritor = new StreamWriter(destino, Utf8ComBom, bufferSize: 64 * 1024, leaveOpen: true) { NewLine = "\r\n" };

        await escritor.WriteLineAsync(string.Join(Separador, Cabecalho));

        await foreach (var sku in gradeRepository.ListarTodosSkusOrfaosAsync(termo?.Trim()).WithCancellation(cancellationToken))
        {
            await escritor.WriteLineAsync(string.Join(Separador,
                EscaparCampo(sku.CodigoSku),
                EscaparCampo(sku.Descricao),
                StatusSkuOrfao,
                dataExportacao));
        }

        await escritor.FlushAsync(cancellationToken);
    }

    // RFC 4180: campo com separador, aspas ou quebra de linha vai entre aspas,
    // com as aspas internas duplicadas. Valores iniciados por = + - @ ganham um
    // apóstrofo na frente para o Excel não executá-los como fórmula (CSV injection).
    private static string EscaparCampo(string valor)
    {
        if (valor.Length > 0 && valor[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            valor = "'" + valor;

        if (valor.IndexOfAny(CaracteresQueExigemAspas) < 0)
            return valor;

        return $"\"{valor.Replace("\"", "\"\"")}\"";
    }
}

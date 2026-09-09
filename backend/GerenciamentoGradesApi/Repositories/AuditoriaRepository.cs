using System.Data;
using System.Text.Json;
using Dapper;
using GerenciamentoGradesApi.Models;
using GerenciamentoGradesApi.Repositories.Interfaces;

namespace GerenciamentoGradesApi.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    public async Task RegistrarAsync(AuditoriaRegistro registro, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = """
            INSERT INTO grade_precos_auditoria
                (TIPO_OPERACAO, CODIGO_GRADE, SKU, ESTADO_ANTERIOR, ESTADO_NOVO, MATRICULA_USUARIO)
            VALUES
                (@TipoOperacao, @CodigoGrade, @Sku, @EstadoAnterior, @EstadoNovo, @Matricula)
            """;

        await connection.ExecuteAsync(sql, new
        {
            registro.TipoOperacao,
            registro.CodigoGrade,
            registro.Sku,
            EstadoAnterior = Serializar(registro.EstadoAnterior),
            EstadoNovo = Serializar(registro.EstadoNovo),
            registro.Matricula
        }, transaction);
    }

    private static string? Serializar(object? valor) => valor is null ? null : JsonSerializer.Serialize(valor);
}

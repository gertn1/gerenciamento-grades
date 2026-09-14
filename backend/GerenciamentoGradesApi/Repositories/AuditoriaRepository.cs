using System.Text.Json;
using GerenciamentoGradesApi.Data;
using GerenciamentoGradesApi.Models;
using GerenciamentoGradesApi.Repositories.Interfaces;

namespace GerenciamentoGradesApi.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    public void Registrar(AuditoriaRegistro registro, GradesDbContext context)
    {
        context.Auditorias.Add(new GradeAuditoria
        {
            TipoOperacao = registro.TipoOperacao,
            CodigoGrade = registro.CodigoGrade,
            Sku = registro.Sku,
            EstadoAnterior = Serializar(registro.EstadoAnterior),
            EstadoNovo = Serializar(registro.EstadoNovo),
            MatriculaUsuario = registro.Matricula
        });
    }

    private static string? Serializar(object? valor) => valor is null ? null : JsonSerializer.Serialize(valor);
}

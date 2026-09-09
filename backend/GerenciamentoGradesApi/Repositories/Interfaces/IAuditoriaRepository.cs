using System.Data;
using GerenciamentoGradesApi.Models;

namespace GerenciamentoGradesApi.Repositories.Interfaces;

public interface IAuditoriaRepository
{
    Task RegistrarAsync(AuditoriaRegistro registro, IDbConnection connection, IDbTransaction transaction);
}

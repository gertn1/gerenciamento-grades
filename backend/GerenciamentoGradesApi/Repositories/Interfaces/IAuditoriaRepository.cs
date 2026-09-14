using GerenciamentoGradesApi.Data;
using GerenciamentoGradesApi.Models;

namespace GerenciamentoGradesApi.Repositories.Interfaces;

public interface IAuditoriaRepository
{
    // Só adiciona a linha ao change tracker do `context` — quem chama
    // (GradeRepository) decide quando dar SaveChangesAsync/commit, para que a
    // auditoria seja persistida atomicamente junto com a alteração que ela
    // descreve.
    void Registrar(AuditoriaRegistro registro, GradesDbContext context);
}

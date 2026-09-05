using System.Data;

namespace GerenciamentoGradesApi.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

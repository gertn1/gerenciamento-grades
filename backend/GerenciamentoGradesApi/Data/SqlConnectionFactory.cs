using System.Data;
using Microsoft.Data.SqlClient;

namespace GerenciamentoGradesApi.Data;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("COSMOS")
            ?? throw new InvalidOperationException("Connection string 'COSMOS' não configurada.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}

using Dapper;
using GerenciamentoGradesApi.Data;
using GerenciamentoGradesApi.Models;
using GerenciamentoGradesApi.Repositories.Interfaces;

namespace GerenciamentoGradesApi.Repositories;

public class GradeRepository : IGradeRepository
{
    // SQL Server aceita até ~2100 parâmetros por comando; lotes de 1000 mantêm margem segura.
    private const int TamanhoLote = 1000;

    private readonly IDbConnectionFactory _connectionFactory;

    public GradeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<GradeListItem>> ListarAsync(int? codigo, string? nome)
    {
        const string sql = """
            SELECT g.CODIGO AS Codigo, g.NOME AS Nome, g.SIGLA AS Sigla,
                   (SELECT COUNT(1) FROM PRODUTO_MESTRE pm WITH (NOLOCK) WHERE pm.CODIGO_GRADE_PRECOS = g.CODIGO) AS QtdSkus
            FROM grade_precos g WITH (NOLOCK)
            WHERE (@Codigo IS NULL OR g.CODIGO = @Codigo)
              AND (@Nome IS NULL OR g.NOME LIKE '%' + @Nome + '%')
            ORDER BY g.CODIGO
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<GradeListItem>(sql, new { Codigo = codigo, Nome = nome });
    }

    public async Task<Grade?> ObterPorCodigoAsync(int codigo)
    {
        const string sql = """
            SELECT CODIGO AS Codigo, NOME AS Nome, SIGLA AS Sigla
            FROM grade_precos WITH (NOLOCK)
            WHERE CODIGO = @Codigo
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Grade>(sql, new { Codigo = codigo });
    }

    public async Task<IEnumerable<SkuResumo>> ListarSkusPorGradeAsync(int codigo)
    {
        const string sql = """
            SELECT PRME_CD_PRODUTO AS Codigo, PRME_TX_DESCRICAO1 AS Descricao
            FROM PRODUTO_MESTRE WITH (NOLOCK)
            WHERE CODIGO_GRADE_PRECOS = @Codigo
            ORDER BY PRME_CD_PRODUTO
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SkuResumo>(sql, new { Codigo = codigo });
    }

    public async Task<IEnumerable<SkuResumo>> BuscarSkusDisponiveisAsync(string termo, int gradeCodigoAtual)
    {
        const string sql = """
            SELECT TOP 50 PRME_CD_PRODUTO AS Codigo, PRME_TX_DESCRICAO1 AS Descricao
            FROM PRODUTO_MESTRE WITH (NOLOCK)
            WHERE (CODIGO_GRADE_PRECOS IS NULL OR CODIGO_GRADE_PRECOS <> @GradeCodigoAtual)
              AND (
                    CONVERT(varchar(20), PRME_CD_PRODUTO) LIKE @Termo + '%'
                    OR PRME_TX_DESCRICAO1 LIKE '%' + @Termo + '%'
                  )
            ORDER BY PRME_TX_DESCRICAO1
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SkuResumo>(sql, new { Termo = termo, GradeCodigoAtual = gradeCodigoAtual });
    }

    public async Task<int?> ObterCodigoPorNomeAsync(string nome)
    {
        const string sql = """
            SELECT TOP 1 CODIGO
            FROM grade_precos WITH (NOLOCK)
            WHERE NOME = @Nome
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<int?>(sql, new { Nome = nome });
    }

    public async Task<int> CriarAsync(string nome, string sigla)
    {
        // CODIGO é gerado pelo banco (IDENTITY/DEFAULT) — nunca é enviado no INSERT.
        const string sql = """
            INSERT INTO grade_precos (NOME, SIGLA)
            OUTPUT INSERTED.CODIGO
            VALUES (@Nome, @Sigla)
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, new { Nome = nome, Sigla = sigla });
    }

    public async Task<bool> AtualizarAsync(int codigo, string nome, string sigla)
    {
        const string sql = """
            UPDATE grade_precos
            SET NOME = @Nome, SIGLA = @Sigla
            WHERE CODIGO = @Codigo
            """;

        using var connection = _connectionFactory.CreateConnection();
        var linhasAfetadas = await connection.ExecuteAsync(sql, new { Codigo = codigo, Nome = nome, Sigla = sigla });
        return linhasAfetadas > 0;
    }

    public async Task<bool> ExcluirAsync(int codigo)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(
                "UPDATE PRODUTO_MESTRE SET CODIGO_GRADE_PRECOS = NULL WHERE CODIGO_GRADE_PRECOS = @Codigo",
                new { Codigo = codigo },
                transaction);

            var linhasAfetadas = await connection.ExecuteAsync(
                "DELETE FROM grade_precos WHERE CODIGO = @Codigo",
                new { Codigo = codigo },
                transaction);

            transaction.Commit();
            return linhasAfetadas > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<HashSet<string>> FiltrarSkusExistentesAsync(IEnumerable<string> skus)
    {
        const string sql = """
            SELECT PRME_CD_PRODUTO
            FROM PRODUTO_MESTRE WITH (NOLOCK)
            WHERE PRME_CD_PRODUTO IN @Skus
            """;

        var encontrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var connection = _connectionFactory.CreateConnection();

        foreach (var lote in skus.Distinct().Chunk(TamanhoLote))
        {
            var resultado = await connection.QueryAsync<string>(sql, new { Skus = lote });
            encontrados.UnionWith(resultado);
        }

        return encontrados;
    }

    public async Task VincularSkusAsync(int codigo, IEnumerable<string> skus)
    {
        const string sql = """
            UPDATE PRODUTO_MESTRE
            SET CODIGO_GRADE_PRECOS = @Codigo
            WHERE PRME_CD_PRODUTO IN @Skus
            """;

        using var connection = _connectionFactory.CreateConnection();

        foreach (var lote in skus.Distinct().Chunk(TamanhoLote))
        {
            await connection.ExecuteAsync(sql, new { Codigo = codigo, Skus = lote });
        }
    }

    public async Task<HashSet<string>> FiltrarSkusVinculadosAsync(int codigo, IEnumerable<string> skus)
    {
        const string sql = """
            SELECT PRME_CD_PRODUTO
            FROM PRODUTO_MESTRE WITH (NOLOCK)
            WHERE CODIGO_GRADE_PRECOS = @Codigo AND PRME_CD_PRODUTO IN @Skus
            """;

        var vinculados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var connection = _connectionFactory.CreateConnection();

        foreach (var lote in skus.Distinct().Chunk(TamanhoLote))
        {
            var resultado = await connection.QueryAsync<string>(sql, new { Codigo = codigo, Skus = lote });
            vinculados.UnionWith(resultado);
        }

        return vinculados;
    }

    public async Task DesvincularSkusAsync(int codigo, IEnumerable<string> skus)
    {
        const string sql = """
            UPDATE PRODUTO_MESTRE
            SET CODIGO_GRADE_PRECOS = NULL
            WHERE CODIGO_GRADE_PRECOS = @Codigo AND PRME_CD_PRODUTO IN @Skus
            """;

        using var connection = _connectionFactory.CreateConnection();

        foreach (var lote in skus.Distinct().Chunk(TamanhoLote))
        {
            await connection.ExecuteAsync(sql, new { Codigo = codigo, Skus = lote });
        }
    }
}

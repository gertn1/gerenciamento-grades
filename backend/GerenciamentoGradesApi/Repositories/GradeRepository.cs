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
    private readonly IAuditoriaRepository _auditoriaRepository;

    public GradeRepository(IDbConnectionFactory connectionFactory, IAuditoriaRepository auditoriaRepository)
    {
        _connectionFactory = connectionFactory;
        _auditoriaRepository = auditoriaRepository;
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

    public async Task<int> CriarAsync(string nome, string sigla, string matricula)
    {
        // CODIGO é gerado pelo banco (IDENTITY/DEFAULT) — nunca é enviado no INSERT.
        const string sql = """
            INSERT INTO grade_precos (NOME, SIGLA)
            OUTPUT INSERTED.CODIGO
            VALUES (@Nome, @Sigla)
            """;

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var codigo = await connection.QuerySingleAsync<int>(sql, new { Nome = nome, Sigla = sigla }, transaction);

            await _auditoriaRepository.RegistrarAsync(new AuditoriaRegistro
            {
                TipoOperacao = "INSERT",
                CodigoGrade = codigo,
                EstadoAnterior = null,
                EstadoNovo = new { Nome = nome, Sigla = sigla },
                Matricula = matricula
            }, connection, transaction);

            transaction.Commit();
            return codigo;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> AtualizarAsync(int codigo, string nome, string sigla, string matricula)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var anterior = await connection.QuerySingleOrDefaultAsync<Grade>(
                "SELECT CODIGO AS Codigo, NOME AS Nome, SIGLA AS Sigla FROM grade_precos WHERE CODIGO = @Codigo",
                new { Codigo = codigo },
                transaction);

            if (anterior is null)
            {
                transaction.Rollback();
                return false;
            }

            var linhasAfetadas = await connection.ExecuteAsync(
                "UPDATE grade_precos SET NOME = @Nome, SIGLA = @Sigla WHERE CODIGO = @Codigo",
                new { Codigo = codigo, Nome = nome, Sigla = sigla },
                transaction);

            await _auditoriaRepository.RegistrarAsync(new AuditoriaRegistro
            {
                TipoOperacao = "UPDATE",
                CodigoGrade = codigo,
                EstadoAnterior = new { anterior.Nome, anterior.Sigla },
                EstadoNovo = new { Nome = nome, Sigla = sigla },
                Matricula = matricula
            }, connection, transaction);

            transaction.Commit();
            return linhasAfetadas > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ExcluirAsync(int codigo, string matricula)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var anterior = await connection.QuerySingleOrDefaultAsync<Grade>(
                "SELECT CODIGO AS Codigo, NOME AS Nome, SIGLA AS Sigla FROM grade_precos WHERE CODIGO = @Codigo",
                new { Codigo = codigo },
                transaction);

            if (anterior is null)
            {
                transaction.Rollback();
                return false;
            }

            await connection.ExecuteAsync(
                "UPDATE PRODUTO_MESTRE SET CODIGO_GRADE_PRECOS = NULL WHERE CODIGO_GRADE_PRECOS = @Codigo",
                new { Codigo = codigo },
                transaction);

            var linhasAfetadas = await connection.ExecuteAsync(
                "DELETE FROM grade_precos WHERE CODIGO = @Codigo",
                new { Codigo = codigo },
                transaction);

            await _auditoriaRepository.RegistrarAsync(new AuditoriaRegistro
            {
                TipoOperacao = "DELETE",
                CodigoGrade = codigo,
                EstadoAnterior = new { anterior.Nome, anterior.Sigla },
                EstadoNovo = null,
                Matricula = matricula
            }, connection, transaction);

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

    public async Task<Dictionary<string, SkuVinculo>> ObterVinculoAtualAsync(IEnumerable<string> skus)
    {
        const string sql = """
            SELECT pm.PRME_CD_PRODUTO AS Sku, pm.CODIGO_GRADE_PRECOS AS CodigoGrade, g.NOME AS NomeGrade
            FROM PRODUTO_MESTRE pm WITH (NOLOCK)
            LEFT JOIN grade_precos g WITH (NOLOCK) ON g.CODIGO = pm.CODIGO_GRADE_PRECOS
            WHERE pm.PRME_CD_PRODUTO IN @Skus
            """;

        var resultado = new Dictionary<string, SkuVinculo>(StringComparer.OrdinalIgnoreCase);
        using var connection = _connectionFactory.CreateConnection();

        foreach (var lote in skus.Distinct().Chunk(TamanhoLote))
        {
            var linhas = await connection.QueryAsync<SkuVinculo>(sql, new { Skus = lote });
            foreach (var linha in linhas)
                resultado[linha.Sku] = linha;
        }

        return resultado;
    }

    public async Task VincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula)
    {
        const string selecionarAnteriores = """
            SELECT PRME_CD_PRODUTO AS Sku, CODIGO_GRADE_PRECOS AS CodigoGradeAnterior
            FROM PRODUTO_MESTRE
            WHERE PRME_CD_PRODUTO IN @Skus
            """;

        const string atualizar = """
            UPDATE PRODUTO_MESTRE
            SET CODIGO_GRADE_PRECOS = @Codigo
            WHERE PRME_CD_PRODUTO IN @Skus
            """;

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        foreach (var lote in skus.Distinct().Chunk(TamanhoLote))
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                var anteriores = (await connection.QueryAsync<SkuGradeAnterior>(selecionarAnteriores, new { Skus = lote }, transaction))
                    .ToDictionary(x => x.Sku, x => x.CodigoGradeAnterior);

                await connection.ExecuteAsync(atualizar, new { Codigo = codigo, Skus = lote }, transaction);

                foreach (var sku in lote)
                {
                    anteriores.TryGetValue(sku, out var codigoAnterior);

                    await _auditoriaRepository.RegistrarAsync(new AuditoriaRegistro
                    {
                        TipoOperacao = "UPDATE",
                        CodigoGrade = codigo,
                        Sku = sku,
                        EstadoAnterior = new { CodigoGrade = codigoAnterior },
                        EstadoNovo = new { CodigoGrade = codigo },
                        Matricula = matricula
                    }, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    // Classe (não record) de propósito: PRME_CD_PRODUTO é INT no banco, mas o
    // restante da API trata SKU como string; o mapeamento por propriedades do
    // Dapper converte int -> string sem problema, o que o mapeamento posicional
    // de um record (que exige o tipo exato do construtor) não faz.
    private sealed class SkuGradeAnterior
    {
        public string Sku { get; set; } = string.Empty;
        public int? CodigoGradeAnterior { get; set; }
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

    public async Task DesvincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula)
    {
        // Assume que os SKUs recebidos já foram filtrados pelo chamador (via
        // FiltrarSkusVinculadosAsync) e realmente estão vinculados a `codigo` —
        // por isso o estado anterior de cada um é conhecido sem precisar reconsultar.
        const string sql = """
            UPDATE PRODUTO_MESTRE
            SET CODIGO_GRADE_PRECOS = NULL
            WHERE CODIGO_GRADE_PRECOS = @Codigo AND PRME_CD_PRODUTO IN @Skus
            """;

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        foreach (var lote in skus.Distinct().Chunk(TamanhoLote))
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                await connection.ExecuteAsync(sql, new { Codigo = codigo, Skus = lote }, transaction);

                foreach (var sku in lote)
                {
                    await _auditoriaRepository.RegistrarAsync(new AuditoriaRegistro
                    {
                        TipoOperacao = "UPDATE",
                        CodigoGrade = codigo,
                        Sku = sku,
                        EstadoAnterior = new { CodigoGrade = codigo },
                        EstadoNovo = new { CodigoGrade = (int?)null },
                        Matricula = matricula
                    }, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

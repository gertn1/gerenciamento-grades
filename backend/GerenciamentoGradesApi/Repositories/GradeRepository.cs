using GerenciamentoGradesApi.Data;
using GerenciamentoGradesApi.Models;
using GerenciamentoGradesApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GerenciamentoGradesApi.Repositories;

public class GradeRepository(GradesDbContext context, IAuditoriaRepository auditoriaRepository) : IGradeRepository
{
    public async Task<IEnumerable<GradeListItem>> ListarAsync(int? codigoGrade, string? nome)
    {
        var query = context.Grades.AsNoTracking().AsQueryable();

        if (codigoGrade is not null)
            query = query.Where(g => g.Codigo == codigoGrade);

        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(g => EF.Functions.Like(g.Nome, "%" + nome + "%"));

        return await query
            .OrderBy(g => g.Codigo)
            .Select(g => new GradeListItem
            {
                CodigoGrade = g.Codigo,
                Nome = g.Nome,
                Sigla = g.Sigla,
                QtdSkus = context.ProdutosMestre.Count(p => p.CodigoGradePrecos == g.Codigo)
            })
            .ToListAsync();
    }

    public async Task<Grade?> ObterPorCodigoAsync(int codigoGrade) =>
        await context.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.Codigo == codigoGrade);

    public async Task<Grade?> ObterPorNomeOuSiglaAsync(string nome, string sigla, int? codigoGradeExcluida = null)
    {
        var query = context.Grades.AsNoTracking().Where(g => g.Nome == nome || g.Sigla == sigla);

        if (codigoGradeExcluida is not null)
            query = query.Where(g => g.Codigo != codigoGradeExcluida);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<Grade?> ObterPorNomeAsync(string nome) =>
        await context.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.Nome == nome);

    public async Task<IEnumerable<SkuResumo>> ListarSkusPorGradeAsync(int codigoGrade)
    {
        return await context.ProdutosMestre.AsNoTracking()
            .Where(p => p.CodigoGradePrecos == codigoGrade)
            .OrderBy(p => p.Codigo)
            .Select(p => new SkuResumo { CodigoSku = p.Codigo.ToString(), Descricao = p.Descricao ?? string.Empty })
            .ToListAsync();
    }

    public async Task<IEnumerable<SkuResumo>> BuscarSkusDisponiveisAsync(string termo, int codigoGradeAtual)
    {
        var query = context.ProdutosMestre.AsNoTracking()
            .Where(p => p.CodigoGradePrecos == null) ;

        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(p => EF.Functions.Like(p.Codigo.ToString(), termo + "%"));
        }

        return await query
            .OrderBy(p => p.Descricao)
            .Take(50)
            .Select(p => new SkuResumo { CodigoSku = p.Codigo.ToString(), Descricao = p.Descricao ?? string.Empty })
            .ToListAsync();
    }

    public async Task<(IEnumerable<SkuResumo> Itens, int Total)> ListarSkusOrfaosAsync(string? termo, int pagina, int tamanhoPagina)
    {
        var query = ConsultarSkusOrfaos(termo);

        var total = await query.CountAsync();

        var itens = await query
            .OrderBy(p => p.Codigo)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(p => new SkuResumo { CodigoSku = p.Codigo.ToString(), Descricao = p.Descricao ?? string.Empty })
            .ToListAsync();

        return (itens, total);
    }

    // Sem paginação e sem materializar a lista: AsAsyncEnumerable entrega as
    // linhas conforme o SQL Server as devolve, e AsNoTracking impede que o
    // change tracker cresça junto com o resultado — é o que permite exportar
    // todos os órfãos em streaming.
    public IAsyncEnumerable<SkuResumo> ListarTodosSkusOrfaosAsync(string? termo) =>
        ConsultarSkusOrfaos(termo)
            .OrderBy(p => p.Codigo)
            .Select(p => new SkuResumo { CodigoSku = p.Codigo.ToString(), Descricao = p.Descricao ?? string.Empty })
            .AsAsyncEnumerable();

    private IQueryable<ProdutoMestre> ConsultarSkusOrfaos(string? termo)
    {
        var query = context.ProdutosMestre.AsNoTracking().Where(p => p.CodigoGradePrecos == null);

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(p => EF.Functions.Like(p.Codigo.ToString(), termo + "%"));

        return query;
    }

    public async Task<IEnumerable<GradeListItem>> ListarGradesVaziasAsync(int? codigoGrade, string? nome)
    {
        var query = context.Grades.AsNoTracking()
            .Where(g => !context.ProdutosMestre.Any(p => p.CodigoGradePrecos == g.Codigo));

        if (codigoGrade is not null)
            query = query.Where(g => g.Codigo == codigoGrade);

        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(g => EF.Functions.Like(g.Nome, "%" + nome + "%"));

        return await query
            .OrderBy(g => g.Codigo)
            .Select(g => new GradeListItem { CodigoGrade = g.Codigo, Nome = g.Nome, Sigla = g.Sigla, QtdSkus = 0 })
            .ToListAsync();
    }

    public async Task<int> CriarAsync(string nome, string sigla, string matricula)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var grade = new Grade { Nome = nome, Sigla = sigla };
            context.Grades.Add(grade);
            await context.SaveChangesAsync();

            auditoriaRepository.Registrar(new AuditoriaRegistro
            {
                TipoOperacao = "INSERT",
                CodigoGrade = grade.Codigo,
                EstadoAnterior = null,
                EstadoNovo = new { Nome = nome, Sigla = sigla },
                Matricula = matricula
            }, context);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return grade.Codigo;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AtualizarAsync(int codigoGrade, string nome, string sigla, string matricula)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var grade = await context.Grades.FirstOrDefaultAsync(g => g.Codigo == codigoGrade);
            if (grade is null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var estadoAnterior = new { grade.Nome, grade.Sigla };
            grade.Nome = nome;
            grade.Sigla = sigla;

            auditoriaRepository.Registrar(new AuditoriaRegistro
            {
                TipoOperacao = "UPDATE",
                CodigoGrade = codigoGrade,
                EstadoAnterior = estadoAnterior,
                EstadoNovo = new { Nome = nome, Sigla = sigla },
                Matricula = matricula
            }, context);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ExcluirAsync(int codigoGrade, string matricula)
    {
        var grade = await context.Grades.FirstOrDefaultAsync(g => g.Codigo == codigoGrade);
        if (grade is null)
            return false;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.ProdutosMestre
                .Where(p => p.CodigoGradePrecos == codigoGrade)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CodigoGradePrecos, (int?)null));

            var estadoAnterior = new { grade.Nome, grade.Sigla };
            context.Grades.Remove(grade);

            auditoriaRepository.Registrar(new AuditoriaRegistro
            {
                TipoOperacao = "DELETE",
                CodigoGrade = codigoGrade,
                EstadoAnterior = estadoAnterior,
                EstadoNovo = {},
                Matricula = matricula
            }, context);

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<HashSet<string>> FiltrarSkusExistentesAsync(IEnumerable<string> skus)
    {
        var codigosSku = ParseCodigosSkuValidos(skus);
        if (codigosSku.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var encontrados = await context.ProdutosMestre.AsNoTracking()
            .Where(p => codigosSku.Contains(p.Codigo))
            .Select(p => p.Codigo)
            .ToListAsync();

        return new HashSet<string>(encontrados.Select(c => c.ToString()), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Dictionary<string, SkuVinculo>> ObterVinculoAtualAsync(IEnumerable<string> skus)
    {
        var codigosSku = ParseCodigosSkuValidos(skus);
        var resultado = new Dictionary<string, SkuVinculo>(StringComparer.OrdinalIgnoreCase);
        if (codigosSku.Count == 0)
            return resultado;

        var linhas = await (
            from p in context.ProdutosMestre.AsNoTracking()
            where codigosSku.Contains(p.Codigo)
            join g in context.Grades.AsNoTracking() on p.CodigoGradePrecos equals g.Codigo into gradesJoin
            from g in gradesJoin.DefaultIfEmpty()
            select new { p.Codigo, p.CodigoGradePrecos, NomeGrade = g != null ? g.Nome : null }
        ).ToListAsync();

        foreach (var linha in linhas)
        {
            var sku = linha.Codigo.ToString();
            resultado[sku] = new SkuVinculo { Sku = sku, CodigoGrade = linha.CodigoGradePrecos, NomeGrade = linha.NomeGrade };
        }

        return resultado;
    }

    public async Task VincularSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula)
    {
        var codigosSku = ParseCodigosSkuValidos(skus);
        if (codigosSku.Count == 0)
            return;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var anteriores = await context.ProdutosMestre
                .Where(p => codigosSku.Contains(p.Codigo))
                .ToDictionaryAsync(p => p.Codigo, p => p.CodigoGradePrecos);

            await context.ProdutosMestre
                .Where(p => codigosSku.Contains(p.Codigo))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CodigoGradePrecos, codigoGrade));

            foreach (var codigoSku in codigosSku)
            {
                anteriores.TryGetValue(codigoSku, out var codigoGradeAnterior);

                auditoriaRepository.Registrar(new AuditoriaRegistro
                {
                    TipoOperacao = "UPDATE",
                    CodigoGrade = codigoGrade,
                    Sku = codigoSku.ToString(),
                    EstadoAnterior = new { CodigoGrade = codigoGradeAnterior },
                    EstadoNovo = new { CodigoGrade = codigoGrade },
                    Matricula = matricula
                }, context);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<HashSet<string>> FiltrarSkusVinculadosAsync(int codigoGrade, IEnumerable<string> skus)
    {
        var codigosSku = ParseCodigosSkuValidos(skus);
        if (codigosSku.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var vinculados = await context.ProdutosMestre.AsNoTracking()
            .Where(p => p.CodigoGradePrecos == codigoGrade && codigosSku.Contains(p.Codigo))
            .Select(p => p.Codigo)
            .ToListAsync();

        return new HashSet<string>(vinculados.Select(c => c.ToString()), StringComparer.OrdinalIgnoreCase);
    }

    public async Task DesvincularSkusAsync(int codigoGrade, IEnumerable<string> skus, string matricula)
    {
        // Assume que os SKUs recebidos já foram filtrados pelo chamador (via
        // FiltrarSkusVinculadosAsync) e realmente estão vinculados a `codigoGrade` —
        // por isso o estado anterior de cada um é conhecido sem precisar reconsultar.
        var codigosSku = ParseCodigosSkuValidos(skus);
        if (codigosSku.Count == 0)
            return;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.ProdutosMestre
                .Where(p => p.CodigoGradePrecos == codigoGrade && codigosSku.Contains(p.Codigo))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CodigoGradePrecos, (int?)null));

            foreach (var codigoSku in codigosSku)
            {
                auditoriaRepository.Registrar(new AuditoriaRegistro
                {
                    TipoOperacao = "UPDATE",
                    CodigoGrade = codigoGrade,
                    Sku = codigoSku.ToString(),
                    EstadoAnterior = new { CodigoGrade = codigoGrade },
                    EstadoNovo = new { CodigoGrade = (int?)null },
                    Matricula = matricula
                }, context);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PRME_CD_PRODUTO é INT no banco; o resto da API trata SKU como string.
    // Entradas que não são um número válido nunca vão "existir" — mesmo
    // comportamento de antes (um SKU inválido só não aparece nos resultados).
    private static List<int> ParseCodigosSkuValidos(IEnumerable<string> skus)
    {
        var codigosSku = new List<int>();
        foreach (var sku in skus.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (int.TryParse(sku, out var codigoSku))
                codigosSku.Add(codigoSku);
        }
        return codigosSku;
    }
}

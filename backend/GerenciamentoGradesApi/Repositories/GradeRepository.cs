using GerenciamentoGradesApi.Data;
using GerenciamentoGradesApi.Models;
using GerenciamentoGradesApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GerenciamentoGradesApi.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly GradesDbContext _context;
    private readonly IAuditoriaRepository _auditoriaRepository;

    public GradeRepository(GradesDbContext context, IAuditoriaRepository auditoriaRepository)
    {
        _context = context;
        _auditoriaRepository = auditoriaRepository;
    }

    public async Task<IEnumerable<GradeListItem>> ListarAsync(int? codigo, string? nome)
    {
        var query = _context.Grades.AsNoTracking().AsQueryable();

        if (codigo is not null)
            query = query.Where(g => g.Codigo == codigo);

        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(g => EF.Functions.Like(g.Nome, "%" + nome + "%"));

        return await query
            .OrderBy(g => g.Codigo)
            .Select(g => new GradeListItem
            {
                Codigo = g.Codigo,
                Nome = g.Nome,
                Sigla = g.Sigla,
                QtdSkus = _context.ProdutosMestre.Count(p => p.CodigoGradePrecos == g.Codigo)
            })
            .ToListAsync();
    }

    public async Task<Grade?> ObterPorCodigoAsync(int codigo) =>
        await _context.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.Codigo == codigo);

    public async Task<Grade?> ObterPorNomeOuSiglaAsync(string nome, string sigla, int? codigoExcluido = null)
    {
        var query = _context.Grades.AsNoTracking().Where(g => g.Nome == nome || g.Sigla == sigla);

        if (codigoExcluido is not null)
            query = query.Where(g => g.Codigo != codigoExcluido);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<Grade?> ObterPorNomeAsync(string nome) =>
        await _context.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.Nome == nome);

    public async Task<IEnumerable<SkuResumo>> ListarSkusPorGradeAsync(int codigo)
    {
        return await _context.ProdutosMestre.AsNoTracking()
            .Where(p => p.CodigoGradePrecos == codigo)
            .OrderBy(p => p.Codigo)
            .Select(p => new SkuResumo { Codigo = p.Codigo.ToString(), Descricao = p.Descricao ?? string.Empty })
            .ToListAsync();
    }

    public async Task<IEnumerable<SkuResumo>> BuscarSkusDisponiveisAsync(string termo, int gradeCodigoAtual)
    {
        var query = _context.ProdutosMestre.AsNoTracking()
            .Where(p => p.CodigoGradePrecos == null || p.CodigoGradePrecos != gradeCodigoAtual);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(p =>
                EF.Functions.Like(p.Codigo.ToString(), termo + "%") ||
                (p.Descricao != null && EF.Functions.Like(p.Descricao, "%" + termo + "%")));
        }

        return await query
            .OrderBy(p => p.Descricao)
            .Take(50)
            .Select(p => new SkuResumo { Codigo = p.Codigo.ToString(), Descricao = p.Descricao ?? string.Empty })
            .ToListAsync();
    }

    public async Task<int> CriarAsync(string nome, string sigla, string matricula)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var grade = new Grade { Nome = nome, Sigla = sigla };
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();

            _auditoriaRepository.Registrar(new AuditoriaRegistro
            {
                TipoOperacao = "INSERT",
                CodigoGrade = grade.Codigo,
                EstadoAnterior = null,
                EstadoNovo = new { Nome = nome, Sigla = sigla },
                Matricula = matricula
            }, _context);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return grade.Codigo;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AtualizarAsync(int codigo, string nome, string sigla, string matricula)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Codigo == codigo);
            if (grade is null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var estadoAnterior = new { grade.Nome, grade.Sigla };
            grade.Nome = nome;
            grade.Sigla = sigla;

            _auditoriaRepository.Registrar(new AuditoriaRegistro
            {
                TipoOperacao = "UPDATE",
                CodigoGrade = codigo,
                EstadoAnterior = estadoAnterior,
                EstadoNovo = new { Nome = nome, Sigla = sigla },
                Matricula = matricula
            }, _context);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ExcluirAsync(int codigo, string matricula)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Codigo == codigo);
            if (grade is null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await _context.ProdutosMestre
                .Where(p => p.CodigoGradePrecos == codigo)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CodigoGradePrecos, (int?)null));

            var estadoAnterior = new { grade.Nome, grade.Sigla };
            _context.Grades.Remove(grade);

            _auditoriaRepository.Registrar(new AuditoriaRegistro
            {
                TipoOperacao = "DELETE",
                CodigoGrade = codigo,
                EstadoAnterior = estadoAnterior,
                EstadoNovo = null,
                Matricula = matricula
            }, _context);

            await _context.SaveChangesAsync();
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
        var codigos = ParseCodigosValidos(skus);
        if (codigos.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var encontrados = await _context.ProdutosMestre.AsNoTracking()
            .Where(p => codigos.Contains(p.Codigo))
            .Select(p => p.Codigo)
            .ToListAsync();

        return new HashSet<string>(encontrados.Select(c => c.ToString()), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Dictionary<string, SkuVinculo>> ObterVinculoAtualAsync(IEnumerable<string> skus)
    {
        var codigos = ParseCodigosValidos(skus);
        var resultado = new Dictionary<string, SkuVinculo>(StringComparer.OrdinalIgnoreCase);
        if (codigos.Count == 0)
            return resultado;

        var linhas = await (
            from p in _context.ProdutosMestre.AsNoTracking()
            where codigos.Contains(p.Codigo)
            join g in _context.Grades.AsNoTracking() on p.CodigoGradePrecos equals g.Codigo into gradesJoin
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

    public async Task VincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula)
    {
        var codigos = ParseCodigosValidos(skus);
        if (codigos.Count == 0)
            return;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var anteriores = await _context.ProdutosMestre
                .Where(p => codigos.Contains(p.Codigo))
                .ToDictionaryAsync(p => p.Codigo, p => p.CodigoGradePrecos);

            await _context.ProdutosMestre
                .Where(p => codigos.Contains(p.Codigo))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CodigoGradePrecos, codigo));

            foreach (var produtoCodigo in codigos)
            {
                anteriores.TryGetValue(produtoCodigo, out var codigoAnterior);

                _auditoriaRepository.Registrar(new AuditoriaRegistro
                {
                    TipoOperacao = "UPDATE",
                    CodigoGrade = codigo,
                    Sku = produtoCodigo.ToString(),
                    EstadoAnterior = new { CodigoGrade = codigoAnterior },
                    EstadoNovo = new { CodigoGrade = codigo },
                    Matricula = matricula
                }, _context);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<HashSet<string>> FiltrarSkusVinculadosAsync(int codigo, IEnumerable<string> skus)
    {
        var codigos = ParseCodigosValidos(skus);
        if (codigos.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var vinculados = await _context.ProdutosMestre.AsNoTracking()
            .Where(p => p.CodigoGradePrecos == codigo && codigos.Contains(p.Codigo))
            .Select(p => p.Codigo)
            .ToListAsync();

        return new HashSet<string>(vinculados.Select(c => c.ToString()), StringComparer.OrdinalIgnoreCase);
    }

    public async Task DesvincularSkusAsync(int codigo, IEnumerable<string> skus, string matricula)
    {
        // Assume que os SKUs recebidos já foram filtrados pelo chamador (via
        // FiltrarSkusVinculadosAsync) e realmente estão vinculados a `codigo` —
        // por isso o estado anterior de cada um é conhecido sem precisar reconsultar.
        var codigos = ParseCodigosValidos(skus);
        if (codigos.Count == 0)
            return;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.ProdutosMestre
                .Where(p => p.CodigoGradePrecos == codigo && codigos.Contains(p.Codigo))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CodigoGradePrecos, (int?)null));

            foreach (var produtoCodigo in codigos)
            {
                _auditoriaRepository.Registrar(new AuditoriaRegistro
                {
                    TipoOperacao = "UPDATE",
                    CodigoGrade = codigo,
                    Sku = produtoCodigo.ToString(),
                    EstadoAnterior = new { CodigoGrade = codigo },
                    EstadoNovo = new { CodigoGrade = (int?)null },
                    Matricula = matricula
                }, _context);
            }

            await _context.SaveChangesAsync();
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
    private static List<int> ParseCodigosValidos(IEnumerable<string> skus)
    {
        var codigos = new List<int>();
        foreach (var sku in skus.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (int.TryParse(sku, out var codigo))
                codigos.Add(codigo);
        }
        return codigos;
    }
}

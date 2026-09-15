using GerenciamentoGradesApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GerenciamentoGradesApi.Data;

// Mapeia para tabelas legadas e compartilhadas com outros sistemas — nunca
// rodar `Database.Migrate()` nem gerar Migrations a partir deste contexto.
// O schema é administrado fora desta API; os scripts de criação ficam em
// backend/database.
public class GradesDbContext(DbContextOptions<GradesDbContext> options) : DbContext(options)
{
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<ProdutoMestre> ProdutosMestre => Set<ProdutoMestre>();
    public DbSet<GradeAuditoria> Auditorias => Set<GradeAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Grade>(entity =>
        {
            entity.ToTable("grade_precos");
            entity.HasKey(g => g.Codigo);
            entity.Property(g => g.Codigo).HasColumnName("CODIGO").ValueGeneratedOnAdd();
            entity.Property(g => g.Nome).HasColumnName("NOME");
            entity.Property(g => g.Sigla).HasColumnName("SIGLA");
        });

        modelBuilder.Entity<ProdutoMestre>(entity =>
        {
            entity.ToTable("PRODUTO_MESTRE");
            entity.HasKey(p => p.Codigo);
            entity.Property(p => p.Codigo).HasColumnName("PRME_CD_PRODUTO").ValueGeneratedNever();
            entity.Property(p => p.Descricao).HasColumnName("PRME_TX_DESCRICAO1");
            entity.Property(p => p.CodigoGradePrecos).HasColumnName("CODIGO_GRADE_PRECOS");
        });

        modelBuilder.Entity<GradeAuditoria>(entity =>
        {
            entity.ToTable("grade_precos_auditoria");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(a => a.DataHora).HasColumnName("DATA_HORA").ValueGeneratedOnAdd();
            entity.Property(a => a.TipoOperacao).HasColumnName("TIPO_OPERACAO");
            entity.Property(a => a.CodigoGrade).HasColumnName("CODIGO_GRADE");
            entity.Property(a => a.Sku).HasColumnName("SKU");
            entity.Property(a => a.EstadoAnterior).HasColumnName("ESTADO_ANTERIOR");
            entity.Property(a => a.EstadoNovo).HasColumnName("ESTADO_NOVO");
            entity.Property(a => a.MatriculaUsuario).HasColumnName("MATRICULA_USUARIO");
        });
    }
}

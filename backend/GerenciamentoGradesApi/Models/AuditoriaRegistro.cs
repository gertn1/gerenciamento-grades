namespace GerenciamentoGradesApi.Models;

public class AuditoriaRegistro
{
    public required string TipoOperacao { get; set; } // INSERT, UPDATE ou DELETE
    public int CodigoGrade { get; set; }
    public string? Sku { get; set; }
    public object? EstadoAnterior { get; set; }
    public object? EstadoNovo { get; set; }
    public required string Matricula { get; set; }
}

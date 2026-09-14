namespace GerenciamentoGradesApi.Models;

// Entidade EF mapeada para grade_precos_auditoria — a linha já persistida
// (estados como JSON serializado em texto). Não confundir com
// AuditoriaRegistro, que é o objeto de entrada usado pelos repositórios para
// pedir o registro de uma auditoria (estados como `object?`, serializados
// para aqui dentro do AuditoriaRepository).
public class GradeAuditoria
{
    public int Id { get; set; }
    public DateTime DataHora { get; set; }
    public required string TipoOperacao { get; set; }
    public int CodigoGrade { get; set; }
    public string? Sku { get; set; }
    public string? EstadoAnterior { get; set; }
    public string? EstadoNovo { get; set; }
    public required string MatriculaUsuario { get; set; }
}

namespace GerenciamentoGradesApi.Models;

public class GradeListItem
{
    public int CodigoGrade { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public int QtdSkus { get; set; }
}

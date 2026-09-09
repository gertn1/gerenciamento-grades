namespace GerenciamentoGradesApi.Models;

// Snapshot do vínculo atual de um SKU: a qual grade ele pertence hoje (se a
// alguma), usado para aplicar a regra "SKU só pode ser vinculado se estiver
// livre ou já pertencer à própria grade de destino".
public class SkuVinculo
{
    public string Sku { get; set; } = string.Empty;
    public int? CodigoGrade { get; set; }
    public string? NomeGrade { get; set; }
}

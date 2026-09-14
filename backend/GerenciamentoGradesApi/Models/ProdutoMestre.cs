namespace GerenciamentoGradesApi.Models;

// Mapeia só as colunas de PRODUTO_MESTRE que este módulo usa — a tabela real
// tem mais de 160 colunas, mas aqui só lemos/atualizamos o vínculo com a
// grade e a descrição do produto. Nunca inserimos nem apagamos linhas desta
// tabela (o produto em si é gerenciado por outro sistema).
public class ProdutoMestre
{
    public int Codigo { get; set; }
    public string? Descricao { get; set; }
    public int? CodigoGradePrecos { get; set; }
}

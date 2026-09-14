export interface Grade {
  codigo: number;
  nome: string;
  sigla: string;
}

export interface GradeListItem extends Grade {
  qtdSkus: number;
}

export interface SkuResumo {
  codigo: string;
  descricao: string;
}

export interface GradeDetalhe extends Grade {
  skus: SkuResumo[];
}

export interface SkuRejeitado {
  sku: string;
  mensagem: string;
}

export interface AtualizarSkusResult {
  grade: GradeDetalhe;
  skusRejeitados: SkuRejeitado[];
}

export interface ErroLinha {
  linha: number;
  mensagem: string;
}

export interface ImportacaoResult {
  totalLinhas: number;
  sucesso: number;
  erros: ErroLinha[];
}

export interface GradeFormValues {
  nome: string;
  sigla: string;
}

export interface SkusOrfaosResult {
  itens: SkuResumo[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

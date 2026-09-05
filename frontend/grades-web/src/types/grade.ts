export interface Grade {
  codigo: number;
  nome: string;
  sigla: string;
}

export interface GradeListItem extends Grade {
  qtdSkus: number;
}

export interface GradeDetalhe extends Grade {
  skus: string[];
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

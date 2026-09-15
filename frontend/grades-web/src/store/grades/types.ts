// Contratos da API de grades (backend GerenciamentoGradesApi) e formato do
// estado de cada slice deste domínio.

export interface Grade {
  codigoGrade: number;
  nome: string;
  sigla: string;
}

export interface GradeListItem extends Grade {
  qtdSkus: number;
}

export interface SkuResumo {
  codigoSku: string;
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

export interface SkusOrfaosResult {
  itens: SkuResumo[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

// Requests

export interface GradeSearchParams {
  codigoGrade?: number;
  nome?: string;
}

export interface GradeFormValues {
  nome: string;
  sigla: string;
}

export interface UpdateGradeRequest {
  codigoGrade: number;
  valores: GradeFormValues;
}

export interface SkusGradeRequest {
  codigoGrade: number;
  skus: string[];
}

export interface SkusDisponiveisParams {
  codigoGrade: number;
  termo: string;
}

export interface SkusOrfaosParams {
  pagina: number;
  tamanhoPagina: number;
  termo?: string;
}

export type TipoImportacaoMassiva = 'criacao' | 'atualizacao' | 'exclusao';

export interface ImportacaoMassivaRequest {
  tipo: TipoImportacaoMassiva;
  arquivo: File;
}

export interface GradeParaExclusao {
  codigoGrade: number;
  nome: string;
  qtdSkus: number;
}

// Estados

export interface GradeState {
  data: GradeListItem[];
  loading: boolean;
  saving: boolean;
  error: string | null;
  searchParams: GradeSearchParams;
}

export const initialGradeState: GradeState = {
  data: [],
  loading: false,
  saving: false,
  error: null,
  searchParams: {},
};

export interface GradeDetailState {
  data: GradeDetalhe | null;
  loading: boolean;
  error: string | null;
  requestId: string | null;
  updatingSkus: boolean;
  skusDisponiveis: SkuResumo[];
  loadingSkusDisponiveis: boolean;
  skusDisponiveisRequestId: string | null;
}

export const initialGradeDetailState: GradeDetailState = {
  data: null,
  loading: false,
  error: null,
  requestId: null,
  updatingSkus: false,
  skusDisponiveis: [],
  loadingSkusDisponiveis: false,
  skusDisponiveisRequestId: null,
};

export interface DiagnosticoState {
  skusOrfaos: SkusOrfaosResult;
  loadingSkusOrfaos: boolean;
  skusOrfaosRequestId: string | null;
  gradesVazias: GradeListItem[];
  loadingGradesVazias: boolean;
  gradesVaziasRequestId: string | null;
  error: string | null;
}

export const initialDiagnosticoState: DiagnosticoState = {
  skusOrfaos: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 },
  loadingSkusOrfaos: false,
  skusOrfaosRequestId: null,
  gradesVazias: [],
  loadingGradesVazias: false,
  gradesVaziasRequestId: null,
  error: null,
};

export interface ImportacaoState {
  data: ImportacaoResult | null;
  loading: boolean;
  error: string | null;
}

export const initialImportacaoState: ImportacaoState = {
  data: null,
  loading: false,
  error: null,
};

import axios from 'axios';
import { garantirMatricula } from '../auth/matricula';
import type {
  AtualizarSkusResult,
  Grade,
  GradeDetalhe,
  GradeFormValues,
  GradeListItem,
  ImportacaoResult,
  SkuResumo,
  SkusOrfaosResult,
} from '../types/grade';

export const baseURL = import.meta.env.VITE_API_GRADES_URL ?? 'http://localhost:5244/api/grades';

const api = axios.create({ baseURL });

const METODOS_QUE_ALTERAM_DADOS = new Set(['post', 'put', 'delete', 'patch']);

// Enquanto não existe login/SSO, toda chamada que altera dados precisa da
// matrícula do usuário para o histórico de auditoria (ver Middleware/MatriculaMiddleware.cs
// no back-end). Se ainda não tivermos uma, `garantirMatricula` abre o modal
// de identificação (MatriculaGate) e só resolve quando o usuário confirmar.
api.interceptors.request.use(async (config) => {
  const metodo = config.method?.toLowerCase();
  if (metodo && METODOS_QUE_ALTERAM_DADOS.has(metodo)) {
    const matricula = await garantirMatricula();
    config.headers.set('X-Matricula', matricula);
  }
  return config;
});

export async function listarGrades(filtro: { codigoGrade?: number; nome?: string }): Promise<GradeListItem[]> {
  const { data } = await api.get<GradeListItem[]>('', { params: filtro });
  return data;
}

export async function obterDetalheGrade(codigoGrade: number): Promise<GradeDetalhe> {
  const { data } = await api.get<GradeDetalhe>(`/${codigoGrade}`);
  return data;
}

export async function criarGrade(valores: GradeFormValues): Promise<Grade> {
  const { data } = await api.post<Grade>('', valores);
  return data;
}

export async function atualizarGrade(codigoGrade: number, valores: GradeFormValues): Promise<Grade> {
  const { data } = await api.put<Grade>(`/${codigoGrade}`, valores);
  return data;
}

export async function excluirGrade(codigoGrade: number): Promise<void> {
  await api.delete(`/${codigoGrade}`);
}

export async function buscarSkusDisponiveis(codigoGrade: number, termo: string): Promise<SkuResumo[]> {
  const { data } = await api.get<SkuResumo[]>(`/${codigoGrade}/skus-disponiveis`, { params: { termo } });
  return data;
}

export async function adicionarSkus(codigoGrade: number, skus: string[]): Promise<AtualizarSkusResult> {
  const { data } = await api.post<AtualizarSkusResult>(`/${codigoGrade}/skus`, { skus });
  return data;
}

export async function removerSkus(codigoGrade: number, skus: string[]): Promise<AtualizarSkusResult> {
  const { data } = await api.post<AtualizarSkusResult>(`/${codigoGrade}/skus/remover`, { skus });
  return data;
}

export async function listarSkusOrfaos(
  pagina: number,
  tamanhoPagina: number,
  termo?: string,
): Promise<SkusOrfaosResult> {
  const { data } = await api.get<SkusOrfaosResult>('/skus-orfaos', { params: { pagina, tamanhoPagina, termo } });
  return data;
}

// Link direto (não passa pelo axios): o navegador baixa o CSV em streaming,
// sem carregar o arquivo inteiro na memória da página.
export function urlExportarSkusOrfaos(termo?: string): string {
  const query = termo ? `?${new URLSearchParams({ termo })}` : '';
  return `${baseURL}/skus-orfaos/exportar${query}`;
}

export async function listarGradesVazias(filtro: { codigoGrade?: number; nome?: string }): Promise<GradeListItem[]> {
  const { data } = await api.get<GradeListItem[]>('/grades-vazias', { params: filtro });
  return data;
}

export function urlModeloImportacaoMassiva(): string {
  return `${baseURL}/importacao-massiva/modelo`;
}

export function urlModeloExclusaoMassivaSkus(): string {
  return `${baseURL}/exclusao-massiva-skus/modelo`;
}

export async function importacaoMassiva(arquivo: File): Promise<ImportacaoResult> {
  const formData = new FormData();
  formData.append('file', arquivo);
  const { data } = await api.post<ImportacaoResult>('/importacao-massiva', formData);
  return data;
}

export async function importacaoMassivaAtualizacao(arquivo: File): Promise<ImportacaoResult> {
  const formData = new FormData();
  formData.append('file', arquivo);
  const { data } = await api.post<ImportacaoResult>('/importacao-massiva-atualizacao', formData);
  return data;
}

export async function exclusaoMassivaSkus(arquivo: File): Promise<ImportacaoResult> {
  const formData = new FormData();
  formData.append('file', arquivo);
  const { data } = await api.post<ImportacaoResult>('/exclusao-massiva-skus', formData);
  return data;
}

export function extrairMensagemErro(error: unknown, mensagemPadrao: string): string {
  if (axios.isAxiosError(error)) {
    const mensagem = (error.response?.data as { mensagem?: string } | undefined)?.mensagem;
    if (mensagem) return mensagem;
  }
  return mensagemPadrao;
}

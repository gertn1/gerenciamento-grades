import axios from 'axios';
import type {
  AtualizarSkusResult,
  Grade,
  GradeDetalhe,
  GradeFormValues,
  GradeListItem,
  ImportacaoResult,
  SkuResumo,
} from '../types/grade';

export const baseURL = import.meta.env.VITE_API_GRADES_URL ?? 'http://localhost:5244/api/grades';

const api = axios.create({ baseURL });

export async function listarGrades(filtro: { codigo?: number; nome?: string }): Promise<GradeListItem[]> {
  const { data } = await api.get<GradeListItem[]>('', { params: filtro });
  return data;
}

export async function obterDetalheGrade(codigo: number): Promise<GradeDetalhe> {
  const { data } = await api.get<GradeDetalhe>(`/${codigo}`);
  return data;
}

export async function criarGrade(valores: GradeFormValues): Promise<Grade> {
  const { data } = await api.post<Grade>('', valores);
  return data;
}

export async function atualizarGrade(codigo: number, valores: GradeFormValues): Promise<Grade> {
  const { data } = await api.put<Grade>(`/${codigo}`, valores);
  return data;
}

export async function excluirGrade(codigo: number): Promise<void> {
  await api.delete(`/${codigo}`);
}

export async function buscarSkusDisponiveis(codigo: number, termo: string): Promise<SkuResumo[]> {
  const { data } = await api.get<SkuResumo[]>(`/${codigo}/skus-disponiveis`, { params: { termo } });
  return data;
}

export async function adicionarSkus(codigo: number, skus: string[]): Promise<AtualizarSkusResult> {
  const { data } = await api.post<AtualizarSkusResult>(`/${codigo}/skus`, { skus });
  return data;
}

export async function removerSkus(codigo: number, skus: string[]): Promise<AtualizarSkusResult> {
  const { data } = await api.post<AtualizarSkusResult>(`/${codigo}/skus/remover`, { skus });
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

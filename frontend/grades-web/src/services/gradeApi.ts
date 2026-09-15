import { createAsyncThunk } from '@reduxjs/toolkit';
import axios from 'axios';
import type { ApiError } from '../store/shared';
import type {
  AtualizarSkusResult,
  Grade,
  GradeDetalhe,
  GradeFormValues,
  GradeListItem,
  GradeSearchParams,
  ImportacaoMassivaRequest,
  ImportacaoResult,
  SkuResumo,
  SkusDisponiveisParams,
  SkusGradeRequest,
  SkusOrfaosParams,
  SkusOrfaosResult,
  TipoImportacaoMassiva,
  UpdateGradeRequest,
} from '../store/grades/types';
import { garantirMatricula } from '../utils/matricula';

const endpoint = process.env.REACT_APP_API_GRADES;

// Instância própria (e não o axios global, como nos demais services do
// pricing-webapp) só para anexar a matrícula às escritas sem afetar as
// requisições dos outros módulos quando este for acoplado ao precoWeb.
const api = axios.create({ baseURL: endpoint });

const METODOS_QUE_ALTERAM_DADOS = new Set(['post', 'put', 'delete', 'patch']);

api.interceptors.request.use(async (config) => {
  if (METODOS_QUE_ALTERAM_DADOS.has(config.method?.toLowerCase() ?? '')) {
    config.headers.set('X-Matricula', await garantirMatricula());
  }
  return config;
});

// O back-end devolve { mensagem } nos erros de negócio (400, 404, 409).
const toApiError = (error: unknown, defaultMessage: string): ApiError => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { mensagem?: string } | undefined;
    return { message: data?.mensagem ?? defaultMessage, status: error.response?.status };
  }
  return { message: defaultMessage };
};

type ThunkConfig = { rejectValue: ApiError };

const ROTAS_IMPORTACAO: Record<TipoImportacaoMassiva, string> = {
  criacao: '/criacao-massiva',
  atualizacao: '/importacao-massiva-atualizacao',
  exclusao: '/exclusao-massiva-skus',
};

export const urlModeloImportacao = (tipo: TipoImportacaoMassiva): string =>
  `${endpoint}${ROTAS_IMPORTACAO[tipo]}/modelo`;

// Link direto (não passa pelo axios): o navegador baixa o CSV em streaming,
// sem carregar o arquivo inteiro na memória da página.
export const urlExportarSkusOrfaos = (termo?: string): string => {
  const query = termo ? `?${new URLSearchParams({ termo })}` : '';
  return `${endpoint}/skus-orfaos/exportar${query}`;
};

// Grades

export const fetchGrades = createAsyncThunk<GradeListItem[], GradeSearchParams, ThunkConfig>(
  'grades/fetchGrades',
  async (params, { rejectWithValue }) => {
    try {
      const { data } = await api.get<GradeListItem[]>('', { params });
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível carregar as grades.'));
    }
  },
);

export const fetchGradeDetail = createAsyncThunk<GradeDetalhe, number, ThunkConfig>(
  'grades/fetchGradeDetail',
  async (codigoGrade, { rejectWithValue }) => {
    try {
      const { data } = await api.get<GradeDetalhe>(`/${codigoGrade}`);
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível carregar a grade.'));
    }
  },
);

export const fetchCreateGrade = createAsyncThunk<Grade, GradeFormValues, ThunkConfig>(
  'grades/fetchCreateGrade',
  async (valores, { rejectWithValue }) => {
    try {
      const { data } = await api.post<Grade>('', valores);
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível criar a grade.'));
    }
  },
);

export const fetchUpdateGrade = createAsyncThunk<Grade, UpdateGradeRequest, ThunkConfig>(
  'grades/fetchUpdateGrade',
  async ({ codigoGrade, valores }, { rejectWithValue }) => {
    try {
      const { data } = await api.put<Grade>(`/${codigoGrade}`, valores);
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível atualizar a grade.'));
    }
  },
);

export const fetchDeleteGrade = createAsyncThunk<number, number, ThunkConfig>(
  'grades/fetchDeleteGrade',
  async (codigoGrade, { rejectWithValue }) => {
    try {
      await api.delete(`/${codigoGrade}`);
      return codigoGrade;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível excluir a grade.'));
    }
  },
);

// SKUs da grade

export const fetchSkusDisponiveis = createAsyncThunk<SkuResumo[], SkusDisponiveisParams, ThunkConfig>(
  'grades/fetchSkusDisponiveis',
  async ({ codigoGrade, termo }, { rejectWithValue }) => {
    try {
      const { data } = await api.get<SkuResumo[]>(`/${codigoGrade}/skus-disponiveis`, { params: { termo } });
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível buscar SKUs.'));
    }
  },
);

export const fetchAddSkus = createAsyncThunk<AtualizarSkusResult, SkusGradeRequest, ThunkConfig>(
  'grades/fetchAddSkus',
  async ({ codigoGrade, skus }, { rejectWithValue }) => {
    try {
      const { data } = await api.post<AtualizarSkusResult>(`/${codigoGrade}/skus`, { skus });
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível adicionar os SKUs.'));
    }
  },
);

export const fetchRemoveSkus = createAsyncThunk<AtualizarSkusResult, SkusGradeRequest, ThunkConfig>(
  'grades/fetchRemoveSkus',
  async ({ codigoGrade, skus }, { rejectWithValue }) => {
    try {
      const { data } = await api.post<AtualizarSkusResult>(`/${codigoGrade}/skus/remover`, { skus });
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível remover os SKUs.'));
    }
  },
);

// Diagnóstico

export const fetchSkusOrfaos = createAsyncThunk<SkusOrfaosResult, SkusOrfaosParams, ThunkConfig>(
  'grades/fetchSkusOrfaos',
  async (params, { rejectWithValue }) => {
    try {
      const { data } = await api.get<SkusOrfaosResult>('/skus-orfaos', { params });
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível carregar os SKUs órfãos.'));
    }
  },
);

export const fetchGradesVazias = createAsyncThunk<GradeListItem[], GradeSearchParams, ThunkConfig>(
  'grades/fetchGradesVazias',
  async (params, { rejectWithValue }) => {
    try {
      const { data } = await api.get<GradeListItem[]>('/grades-vazias', { params });
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível carregar as grades vazias.'));
    }
  },
);

// Operações massivas (planilhas .xlsx)

export const fetchImportacaoMassiva = createAsyncThunk<ImportacaoResult, ImportacaoMassivaRequest, ThunkConfig>(
  'grades/fetchImportacaoMassiva',
  async ({ tipo, arquivo }, { rejectWithValue }) => {
    try {
      const formData = new FormData();
      formData.append('file', arquivo);
      const { data } = await api.post<ImportacaoResult>(ROTAS_IMPORTACAO[tipo], formData);
      return data;
    } catch (error) {
      return rejectWithValue(toApiError(error, 'Não foi possível processar o arquivo.'));
    }
  },
);

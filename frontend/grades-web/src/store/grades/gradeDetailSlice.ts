import { createSlice, isAnyOf } from '@reduxjs/toolkit';
import {
  fetchAddSkus,
  fetchDeleteGrade,
  fetchGradeDetail,
  fetchRemoveSkus,
  fetchSkusDisponiveis,
  fetchUpdateGrade,
} from '../../services/gradeApi';
import { initialGradeDetailState } from './types';

const gradeDetailSlice = createSlice({
  name: 'gradeDetail',
  initialState: initialGradeDetailState,
  reducers: {
    clearGradeDetail: () => initialGradeDetailState,
    clearSkusDisponiveis: (state) => {
      state.skusDisponiveis = [];
      state.loadingSkusDisponiveis = false;
      state.skusDisponiveisRequestId = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchGradeDetail.pending, (state, action) => {
        state.loading = true;
        state.error = null;
        state.data = null;
        state.requestId = action.meta.requestId;
      })
      .addCase(fetchGradeDetail.fulfilled, (state, action) => {
        // Ignora respostas de uma grade que já foi fechada/trocada.
        if (action.meta.requestId !== state.requestId) return;
        state.loading = false;
        state.data = action.payload;
      })
      .addCase(fetchGradeDetail.rejected, (state, action) => {
        if (action.meta.requestId !== state.requestId) return;
        state.loading = false;
        state.error = action.payload?.message ?? action.error.message ?? null;
      })
      .addCase(fetchSkusDisponiveis.pending, (state, action) => {
        state.loadingSkusDisponiveis = true;
        state.skusDisponiveisRequestId = action.meta.requestId;
      })
      .addCase(fetchSkusDisponiveis.fulfilled, (state, action) => {
        // Busca com debounce: só a resposta do termo mais recente vale.
        if (action.meta.requestId !== state.skusDisponiveisRequestId) return;
        state.loadingSkusDisponiveis = false;
        state.skusDisponiveis = action.payload;
      })
      .addCase(fetchSkusDisponiveis.rejected, (state, action) => {
        if (action.meta.requestId !== state.skusDisponiveisRequestId) return;
        state.loadingSkusDisponiveis = false;
      })
      .addCase(fetchUpdateGrade.fulfilled, (state, action) => {
        if (state.data?.codigoGrade === action.payload.codigoGrade) {
          state.data.nome = action.payload.nome;
          state.data.sigla = action.payload.sigla;
        }
      })
      .addCase(fetchDeleteGrade.fulfilled, (state, action) => {
        if (state.data?.codigoGrade === action.payload) return initialGradeDetailState;
      })
      .addMatcher(isAnyOf(fetchAddSkus.pending, fetchRemoveSkus.pending), (state) => {
        state.updatingSkus = true;
      })
      .addMatcher(isAnyOf(fetchAddSkus.fulfilled, fetchRemoveSkus.fulfilled), (state, action) => {
        state.updatingSkus = false;
        state.data = action.payload.grade;
      })
      .addMatcher(isAnyOf(fetchAddSkus.rejected, fetchRemoveSkus.rejected), (state) => {
        state.updatingSkus = false;
      });
  },
});

export const { clearGradeDetail, clearSkusDisponiveis } = gradeDetailSlice.actions;

export default gradeDetailSlice.reducer;

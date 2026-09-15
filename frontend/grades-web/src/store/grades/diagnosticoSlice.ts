import { createSlice } from '@reduxjs/toolkit';
import { fetchGradesVazias, fetchSkusOrfaos } from '../../services/gradeApi';
import { initialDiagnosticoState } from './types';

// As duas buscas são disparadas com debounce enquanto o usuário digita: cada
// uma guarda o requestId da chamada mais recente e descarta respostas antigas.
const diagnosticoSlice = createSlice({
  name: 'diagnostico',
  initialState: initialDiagnosticoState,
  reducers: {
    clearDiagnostico: () => initialDiagnosticoState,
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchSkusOrfaos.pending, (state, action) => {
        state.loadingSkusOrfaos = true;
        state.skusOrfaosRequestId = action.meta.requestId;
        state.error = null;
      })
      .addCase(fetchSkusOrfaos.fulfilled, (state, action) => {
        if (action.meta.requestId !== state.skusOrfaosRequestId) return;
        state.loadingSkusOrfaos = false;
        state.skusOrfaos = action.payload;
      })
      .addCase(fetchSkusOrfaos.rejected, (state, action) => {
        if (action.meta.requestId !== state.skusOrfaosRequestId) return;
        state.loadingSkusOrfaos = false;
        state.error = action.payload?.message ?? action.error.message ?? null;
      })
      .addCase(fetchGradesVazias.pending, (state, action) => {
        state.loadingGradesVazias = true;
        state.gradesVaziasRequestId = action.meta.requestId;
        state.error = null;
      })
      .addCase(fetchGradesVazias.fulfilled, (state, action) => {
        if (action.meta.requestId !== state.gradesVaziasRequestId) return;
        state.loadingGradesVazias = false;
        state.gradesVazias = action.payload;
      })
      .addCase(fetchGradesVazias.rejected, (state, action) => {
        if (action.meta.requestId !== state.gradesVaziasRequestId) return;
        state.loadingGradesVazias = false;
        state.error = action.payload?.message ?? action.error.message ?? null;
      });
  },
});

export const { clearDiagnostico } = diagnosticoSlice.actions;

export default diagnosticoSlice.reducer;

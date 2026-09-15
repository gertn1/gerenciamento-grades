import { createSlice, isAnyOf } from '@reduxjs/toolkit';
import { fetchCreateGrade, fetchDeleteGrade, fetchGrades, fetchUpdateGrade } from '../../services/gradeApi';
import { initialGradeState } from './types';

const gradeSlice = createSlice({
  name: 'grades',
  initialState: initialGradeState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchGrades.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchGrades.fulfilled, (state, action) => {
        state.loading = false;
        state.data = action.payload;
        state.searchParams = action.meta.arg;
      })
      .addCase(fetchGrades.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload?.message ?? action.error.message ?? null;
      })
      .addCase(fetchUpdateGrade.fulfilled, (state, action) => {
        const grade = state.data.find((g) => g.codigoGrade === action.payload.codigoGrade);
        if (grade) {
          grade.nome = action.payload.nome;
          grade.sigla = action.payload.sigla;
        }
      })
      .addCase(fetchDeleteGrade.fulfilled, (state, action) => {
        state.data = state.data.filter((g) => g.codigoGrade !== action.payload);
      })
      .addMatcher(isAnyOf(fetchCreateGrade.pending, fetchUpdateGrade.pending, fetchDeleteGrade.pending), (state) => {
        state.saving = true;
        state.error = null;
      })
      .addMatcher(isAnyOf(fetchCreateGrade.fulfilled, fetchUpdateGrade.fulfilled, fetchDeleteGrade.fulfilled), (state) => {
        state.saving = false;
      })
      .addMatcher(isAnyOf(fetchCreateGrade.rejected, fetchUpdateGrade.rejected, fetchDeleteGrade.rejected), (state, action) => {
        state.saving = false;
        state.error = action.payload?.message ?? action.error.message ?? null;
      });
  },
});

export default gradeSlice.reducer;

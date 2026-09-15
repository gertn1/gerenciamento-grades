import { createSlice } from '@reduxjs/toolkit';
import { fetchImportacaoMassiva } from '../../services/gradeApi';
import { initialImportacaoState } from './types';

const importacaoSlice = createSlice({
  name: 'importacao',
  initialState: initialImportacaoState,
  reducers: {
    clearImportacao: () => initialImportacaoState,
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchImportacaoMassiva.pending, (state) => {
        state.loading = true;
        state.error = null;
        state.data = null;
      })
      .addCase(fetchImportacaoMassiva.fulfilled, (state, action) => {
        state.loading = false;
        state.data = action.payload;
      })
      .addCase(fetchImportacaoMassiva.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload?.message ?? action.error.message ?? null;
      });
  },
});

export const { clearImportacao } = importacaoSlice.actions;

export default importacaoSlice.reducer;

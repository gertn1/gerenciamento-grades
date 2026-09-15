import { configureStore } from '@reduxjs/toolkit';
import { useDispatch } from 'react-redux';
import grades from './grades/gradeSlice';
import gradeDetail from './grades/gradeDetailSlice';
import diagnostico from './grades/diagnosticoSlice';
import importacao from './grades/importacaoSlice';

const rootReducer = {
  grades,
  gradeDetail,
  diagnostico,
  importacao,
};

const store = configureStore({
  reducer: rootReducer,
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;
export const useAppDispatch = () => useDispatch<AppDispatch>();

export default store;

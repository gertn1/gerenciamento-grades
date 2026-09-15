// Formato padronizado de erro devolvido pelos thunks via rejectWithValue.
export interface ApiError {
  message: string;
  status?: number;
}

// `unwrap()` rejeita com o ApiError do rejectWithValue ou, se o thunk lançou,
// com um SerializedError — os dois expõem `message`.
export const getErrorMessage = (error: unknown, defaultMessage: string): string => {
  const message = (error as { message?: unknown } | null)?.message;
  return typeof message === 'string' && message ? message : defaultMessage;
};

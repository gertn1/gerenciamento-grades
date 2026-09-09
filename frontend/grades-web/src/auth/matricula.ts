// Stand-in enquanto não existe login/SSO: a matrícula do usuário fica salva
// no navegador (localStorage) e é anexada como cabeçalho X-Matricula em toda
// requisição que altera dados. Se ainda não tiver sido informada, a
// requisição fica pendente até o usuário preenchê-la no modal (ver
// MatriculaGate.tsx), que é quem chama `resolverSolicitacaoPendente`.
const CHAVE_STORAGE = 'grades.matricula';

let matriculaAtual: string | null = lerDoStorage();
let resolverPendente: ((matricula: string) => void) | null = null;
let solicitarAoUsuario: (() => void) | null = null;
const ouvintes = new Set<(matricula: string | null) => void>();

function notificarOuvintes() {
  ouvintes.forEach((ouvinte) => ouvinte(matriculaAtual));
}

// Usado pela UI (ex.: AppHeader) para reagir sem precisar de um estado global.
export function ouvirMatricula(ouvinte: (matricula: string | null) => void): () => void {
  ouvintes.add(ouvinte);
  return () => ouvintes.delete(ouvinte);
}

function lerDoStorage(): string | null {
  try {
    return localStorage.getItem(CHAVE_STORAGE);
  } catch {
    return null;
  }
}

function salvarNoStorage(matricula: string) {
  try {
    localStorage.setItem(CHAVE_STORAGE, matricula);
  } catch {
    // localStorage indisponível (ex.: navegação privada) — mantém apenas em memória.
  }
}

export function validarFormatoMatricula(valor: string): string | null {
  if (!valor.trim()) return 'Informe sua matrícula.';
  if (!/^\d+$/.test(valor.trim())) return 'A matrícula deve conter apenas números.';
  if (valor.trim().length < 3 || valor.trim().length > 20) return 'A matrícula deve ter entre 3 e 20 dígitos.';
  return null;
}

export function obterMatriculaAtual(): string | null {
  return matriculaAtual;
}

export function definirMatricula(matricula: string) {
  matriculaAtual = matricula.trim();
  salvarNoStorage(matriculaAtual);
  notificarOuvintes();
  resolverPendente?.(matriculaAtual);
  resolverPendente = null;
}

export function esquecerMatricula() {
  matriculaAtual = null;
  try {
    localStorage.removeItem(CHAVE_STORAGE);
  } catch {
    // ignora
  }
  notificarOuvintes();
}

// Chamado uma vez pelo MatriculaGate para saber como abrir o modal quando a matrícula for necessária.
export function registrarSolicitador(fn: () => void) {
  solicitarAoUsuario = fn;
}

// Usado pelo interceptor do axios: resolve na hora se já houver matrícula salva,
// senão aciona o modal e só resolve quando o usuário confirmar.
export function garantirMatricula(): Promise<string> {
  if (matriculaAtual) return Promise.resolve(matriculaAtual);

  return new Promise((resolve) => {
    resolverPendente = resolve;
    solicitarAoUsuario?.();
  });
}

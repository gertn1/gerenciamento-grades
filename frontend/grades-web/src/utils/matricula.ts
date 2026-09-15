// Identificação do usuário para a auditoria (cabeçalho X-Matricula).
//
// Dentro do precoWeb o login já grava a matrícula em localStorage.userId (ver
// userSlice do pricing-webapp) — quando ela existe, é usada direto. Executando
// este módulo isolado, a matrícula é pedida no ModalMatricula e guardada no
// navegador; enquanto não é informada, a requisição que precisa dela fica
// pendente até o usuário confirmar.
const CHAVE_STORAGE = 'grades.matricula';
const CHAVE_USUARIO_PRECOWEB = 'userId';
const CHAVE_NOME_PRECOWEB = 'userName';

let matriculaInformada: string | null = lerDoStorage(CHAVE_STORAGE);
let resolverPendente: ((matricula: string) => void) | null = null;
let solicitarAoUsuario: (() => void) | null = null;
const ouvintes = new Set<(matricula: string | null) => void>();

function lerDoStorage(chave: string): string | null {
  try {
    return localStorage.getItem(chave);
  } catch {
    return null;
  }
}

export function validarFormatoMatricula(valor: string): string | null {
  if (!valor.trim()) return 'Informe sua matrícula.';
  if (!/^\d+$/.test(valor.trim())) return 'A matrícula deve conter apenas números.';
  if (valor.trim().length < 3 || valor.trim().length > 20) return 'A matrícula deve ter entre 3 e 20 dígitos.';
  return null;
}

function matriculaDoPrecoWeb(): string | null {
  const userId = lerDoStorage(CHAVE_USUARIO_PRECOWEB)?.trim();
  return userId && validarFormatoMatricula(userId) === null ? userId : null;
}

export function matriculaVemDoPrecoWeb(): boolean {
  return matriculaDoPrecoWeb() !== null;
}

export function obterNomeUsuarioPrecoWeb(): string | null {
  return matriculaVemDoPrecoWeb() ? lerDoStorage(CHAVE_NOME_PRECOWEB) : null;
}

export function obterMatriculaAtual(): string | null {
  return matriculaDoPrecoWeb() ?? matriculaInformada;
}

// Usado pela UI (ex.: Header) para reagir sem precisar de um estado global.
export function ouvirMatricula(ouvinte: (matricula: string | null) => void): () => void {
  ouvintes.add(ouvinte);
  return () => {
    ouvintes.delete(ouvinte);
  };
}

function notificarOuvintes() {
  const atual = obterMatriculaAtual();
  ouvintes.forEach((ouvinte) => ouvinte(atual));
}

export function definirMatricula(matricula: string) {
  matriculaInformada = matricula.trim();
  try {
    localStorage.setItem(CHAVE_STORAGE, matriculaInformada);
  } catch {
    // localStorage indisponível (ex.: navegação privada) — mantém apenas em memória.
  }
  notificarOuvintes();
  resolverPendente?.(matriculaInformada);
  resolverPendente = null;
}

export function esquecerMatricula() {
  matriculaInformada = null;
  try {
    localStorage.removeItem(CHAVE_STORAGE);
  } catch {
    // ignora
  }
  notificarOuvintes();
}

// Chamado uma vez pelo ModalMatricula para saber como abrir o modal quando a matrícula for necessária.
export function registrarSolicitador(fn: () => void) {
  solicitarAoUsuario = fn;
}

// Usado pelo interceptor do axios (services/gradeApi.ts): resolve na hora se
// já houver matrícula, senão aciona o modal e só resolve quando o usuário confirmar.
export function garantirMatricula(): Promise<string> {
  const atual = obterMatriculaAtual();
  if (atual) return Promise.resolve(atual);

  return new Promise((resolve) => {
    resolverPendente = resolve;
    solicitarAoUsuario?.();
  });
}

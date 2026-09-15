import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// Variáveis no mesmo formato do pricing-webapp (CRA): o código lê
// `process.env.REACT_APP_*`. Como o navegador não tem `process`, cada variável
// conhecida é substituída no build a partir dos arquivos .env.<modo>.
const VARIAVEIS_AMBIENTE = ['REACT_APP_NAME', 'REACT_APP_API_GRADES']

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'REACT_APP_')

  return {
    plugins: [react()],
    define: Object.fromEntries(
      VARIAVEIS_AMBIENTE.map((nome) => [`process.env.${nome}`, JSON.stringify(env[nome] ?? '')]),
    ),
  }
})

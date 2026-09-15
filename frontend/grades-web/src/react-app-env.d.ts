// Mesmas variáveis do pricing-webapp (CRA). Aqui no Vite elas são injetadas
// pelo `define` do vite.config.ts; no precoWeb vêm do react-scripts.
declare const process: {
  env: {
    readonly REACT_APP_NAME?: string;
    readonly REACT_APP_API_GRADES?: string;
  };
};

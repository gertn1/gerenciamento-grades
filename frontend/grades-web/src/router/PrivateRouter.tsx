import React from 'react';
import AppFooter from '../components/layout/Footer';
import AppHeader from '../components/layout/Header';

type Props = {
  children: React.ReactNode;
};

// No precoWeb este componente também redireciona para /login quando não há
// usuário logado. Aqui não existe tela de login — a identificação é feita pela
// matrícula (utils/matricula.ts) —, então só o layout é aplicado.
const PrivateRoute: React.FC<Props> = ({ children }) => {
  return (
    <>
      <AppHeader />
      <div style={{ padding: '35px', paddingBottom: '100px', background: '#F8FAFC' }}>{children}</div>
      <AppFooter />
    </>
  );
};

export default PrivateRoute;

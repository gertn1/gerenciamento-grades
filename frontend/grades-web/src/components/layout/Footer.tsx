import React from 'react';
import { Layout } from 'antd';

const { Footer } = Layout;

const AppFooter: React.FC = () => (
  <Footer id="footer" style={{ textAlign: 'center', marginTop: '5px', position: 'absolute', bottom: 0, width: '100%', background: '#F8FAFC' }}>
    Empreendimentos Pague Menos © Todos os direitos reservados - {new Date().getFullYear()}
  </Footer>
);

export default AppFooter;

import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { ConfigProvider } from 'antd';
import locale from 'antd/locale/pt_BR';
import 'antd/dist/reset.css';
import AppRoutes from './router';
import ModalMatricula from './components/Grades/ModalMatricula';

const App: React.FC = () => {
  return (
    <ConfigProvider locale={locale}>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
      <ModalMatricula />
    </ConfigProvider>
  );
};

export default App;

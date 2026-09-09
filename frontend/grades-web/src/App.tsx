import { ConfigProvider, Layout } from 'antd';
import ptBR from 'antd/locale/pt_BR';
import { GradesPage } from './components/GradesPage';
import { MatriculaGate } from './components/MatriculaGate';

function App() {
  return (
    <ConfigProvider locale={ptBR} theme={{ token: { colorPrimary: '#0b3d63' } }}>
      <Layout style={{ minHeight: '100vh', background: '#f0f2f5' }}>
        <GradesPage />
      </Layout>
      <MatriculaGate />
    </ConfigProvider>
  );
}

export default App;

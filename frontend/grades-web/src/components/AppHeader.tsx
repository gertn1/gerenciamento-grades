import { Layout, Space, Typography } from 'antd';

const { Text } = Typography;

export function AppHeader() {
  return (
    <Layout.Header
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 24px',
        background: '#0b3d63',
      }}
    >
      <Space size={10}>
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: 28,
            height: 28,
            borderRadius: 6,
            background: '#e2231a',
            color: '#fff',
            fontWeight: 700,
            fontSize: 18,
            lineHeight: 1,
          }}
        >
          +
        </span>
        <Text strong style={{ color: '#fff', fontSize: 16 }}>
          PagueMenos
        </Text>
      </Space>

      <Text strong style={{ color: '#fff', fontSize: 16 }}>
        Preços WEB
      </Text>

      <Text style={{ color: 'rgba(255,255,255,0.85)' }}>Usuário logado</Text>
    </Layout.Header>
  );
}

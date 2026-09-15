import React from 'react';
import { Layout } from 'antd';

const { Content } = Layout;

const contentStyle: React.CSSProperties = {
  backgroundColor: 'white',
  position: 'relative',
  minHeight: '100vh',
};

type Props = {
  children: React.ReactNode;
};

const Root: React.FC<Props> = ({ children }) => (
  <Layout>
    <Content style={contentStyle}>{children}</Content>
  </Layout>
);

export default Root;

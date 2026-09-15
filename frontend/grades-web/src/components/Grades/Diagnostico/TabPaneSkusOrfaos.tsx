import React, { useEffect, useState } from 'react';
import { Button, Col, Input, Row, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DownloadOutlined, SearchOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import { fetchSkusOrfaos, urlExportarSkusOrfaos } from '../../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../../store';
import type { SkuResumo } from '../../../store/grades/types';
import { alertError } from '../../../utils/alerts';
import { getErrorMessage } from '../../../utils/apiError';

const TAMANHO_PAGINA = 10;

const columns: ColumnsType<SkuResumo> = [
  { title: 'Código', dataIndex: 'codigoSku', width: 110 },
  { title: 'Descrição', dataIndex: 'descricao' },
];

const TabPaneSkusOrfaos: React.FC = () => {
  const dispatch = useAppDispatch();
  const skusOrfaos = useSelector((state: RootState) => state.diagnostico.skusOrfaos);
  const loading = useSelector((state: RootState) => state.diagnostico.loadingSkusOrfaos);

  const [termo, setTermo] = useState('');
  const [pagina, setPagina] = useState(1);

  useEffect(() => {
    const timer = setTimeout(() => {
      dispatch(fetchSkusOrfaos({ pagina, tamanhoPagina: TAMANHO_PAGINA, termo: termo.trim() || undefined }))
        .unwrap()
        .catch((error) => alertError(getErrorMessage(error, 'Não foi possível carregar os SKUs órfãos.')));
    }, 350);

    return () => clearTimeout(timer);
  }, [pagina, termo, dispatch]);

  return (
    <>
      <Typography.Text type="secondary">Produtos sem nenhuma grade vinculada.</Typography.Text>

      <Row gutter={12} style={{ marginTop: 12 }}>
        <Col flex="1 1 auto">
          <Input
            placeholder="Buscar por código do SKU..."
            prefix={<SearchOutlined />}
            value={termo}
            onChange={(e) => {
              setTermo(e.target.value);
              setPagina(1);
            }}
            allowClear
          />
        </Col>
        <Col>
          <Button type="primary" ghost icon={<DownloadOutlined />} href={urlExportarSkusOrfaos(termo.trim() || undefined)} disabled={skusOrfaos.total === 0}>
            Exportar CSV
          </Button>
        </Col>
      </Row>

      <Table
        style={{ marginTop: 12 }}
        size="small"
        bordered
        columns={columns}
        dataSource={skusOrfaos.itens}
        rowKey="codigoSku"
        loading={loading}
        pagination={{
          current: pagina,
          pageSize: TAMANHO_PAGINA,
          total: skusOrfaos.total,
          onChange: setPagina,
          showSizeChanger: false,
        }}
      />
    </>
  );
};

export default TabPaneSkusOrfaos;

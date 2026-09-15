import React, { useEffect, useMemo, useState } from 'react';
import { Button, Empty, Input, Modal, Row, Skeleton, Space, Table, Tooltip, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import ModalAddSkus from './ModalAddSkus';
import { fetchGradeDetail, fetchRemoveSkus } from '../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../store';
import { clearGradeDetail } from '../../store/grades/gradeDetailSlice';
import type { GradeDetalhe, SkuResumo } from '../../store/grades/types';
import { alertError, alertSuccess, confirmAction } from '../../utils/alerts';
import { getErrorMessage } from '../../utils/apiError';

const { Text } = Typography;

type Props = {
  isOpen: boolean;
  codigoGrade: number | null;
  handleCancel: () => void;
  onEditarGrade: (grade: GradeDetalhe) => void;
  onExcluirGrade: (grade: GradeDetalhe) => void;
  onSkusAlterados: () => void;
};

const ModalGradeDetails: React.FC<Props> = ({ isOpen, codigoGrade, handleCancel, onEditarGrade, onExcluirGrade, onSkusAlterados }) => {
  const dispatch = useAppDispatch();
  const detalhe = useSelector((state: RootState) => state.gradeDetail.data);
  const loading = useSelector((state: RootState) => state.gradeDetail.loading);
  const updatingSkus = useSelector((state: RootState) => state.gradeDetail.updatingSkus);

  const [filtro, setFiltro] = useState('');
  const [selecionados, setSelecionados] = useState<string[]>([]);
  const [isAddSkusOpen, setIsAddSkusOpen] = useState(false);

  useEffect(() => {
    if (!isOpen || codigoGrade === null) return;

    dispatch(fetchGradeDetail(codigoGrade))
      .unwrap()
      .catch((error) => alertError(getErrorMessage(error, 'Não foi possível carregar a grade.')));
  }, [isOpen, codigoGrade, dispatch]);

  const handleAfterClose = () => {
    setFiltro('');
    setSelecionados([]);
    dispatch(clearGradeDetail());
  };

  const handleRemoverSkus = async (skus: string[]) => {
    if (!detalhe || skus.length === 0) return;

    const confirmado = await confirmAction(
      skus.length === 1 ? 'Remover SKU da grade?' : `Remover ${skus.length} SKUs da grade?`,
      'O(s) produto(s) removido(s) ficará(ão) sem grade vinculada.',
      'Remover',
    );
    if (!confirmado) return;

    try {
      await dispatch(fetchRemoveSkus({ codigoGrade: detalhe.codigoGrade, skus })).unwrap();
      setSelecionados((atual) => atual.filter((sku) => !skus.includes(sku)));
      alertSuccess(`${skus.length} SKU(s) removido(s) da grade.`);
      onSkusAlterados();
    } catch (error) {
      alertError(getErrorMessage(error, 'Não foi possível remover os SKUs.'));
    }
  };

  const skusFiltrados = useMemo(() => {
    if (!detalhe) return [];
    const termo = filtro.trim().toLowerCase();
    if (!termo) return detalhe.skus;
    return detalhe.skus.filter((sku) => sku.codigoSku.toLowerCase().includes(termo) || sku.descricao.toLowerCase().includes(termo));
  }, [detalhe, filtro]);

  const columns: ColumnsType<SkuResumo> = [
    {
      title: 'Código',
      dataIndex: 'codigoSku',
      width: 110,
      render: (codigoSku: string) => <Text code>{codigoSku}</Text>,
    },
    {
      title: 'Descrição',
      dataIndex: 'descricao',
    },
    {
      title: '',
      width: 60,
      align: 'center',
      render: (_, sku) => (
        <Tooltip title="Remover da grade">
          <Button shape="circle" size="small" danger icon={<DeleteOutlined />} onClick={() => handleRemoverSkus([sku.codigoSku])} />
        </Tooltip>
      ),
    },
  ];

  return (
    <>
      <Modal
        title={detalhe ? detalhe.nome : 'SKUs Vinculados'}
        open={isOpen}
        onCancel={handleCancel}
        afterClose={handleAfterClose}
        width={760}
        destroyOnClose
        footer={
          detalhe && (
            <Row justify="end" style={{ gap: 8 }}>
              <Button icon={<EditOutlined />} onClick={() => onEditarGrade(detalhe)}>
                Editar Grade
              </Button>
              <Button danger icon={<DeleteOutlined />} onClick={() => onExcluirGrade(detalhe)}>
                Excluir Grade
              </Button>
              <Button type="primary" onClick={handleCancel}>
                Fechar
              </Button>
            </Row>
          )
        }
      >
        {loading && <Skeleton active />}

        {!loading && detalhe && (
          <>
            <Text type="secondary">
              Código {detalhe.codigoGrade} · Sigla: {detalhe.sigla}
            </Text>

            <Row justify="space-between" align="middle" style={{ marginTop: 20, marginBottom: 8 }}>
              <Text strong>SKUs Vinculados</Text>
              <Space>
                <Button danger disabled={selecionados.length === 0} onClick={() => handleRemoverSkus(selecionados)}>
                  Remover Selecionados
                </Button>
                <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsAddSkusOpen(true)}>
                  Adicionar SKUs
                </Button>
              </Space>
            </Row>

            <Input
              placeholder="Filtrar SKUs por código ou descrição..."
              prefix={<SearchOutlined />}
              value={filtro}
              onChange={(e) => setFiltro(e.target.value)}
              allowClear
              style={{ marginBottom: 12 }}
              suffix={<Text type="secondary">{skusFiltrados.length} SKU(s)</Text>}
            />

            {detalhe.skus.length === 0 ? (
              <Empty description="Nenhum SKU vinculado a esta grade." />
            ) : (
              <Table
                size="small"
                bordered
                columns={columns}
                dataSource={skusFiltrados}
                rowKey="codigoSku"
                loading={updatingSkus}
                pagination={{ pageSize: 10, hideOnSinglePage: true }}
                rowSelection={{
                  type: 'checkbox',
                  selectedRowKeys: selecionados,
                  onChange: (keys) => setSelecionados(keys as string[]),
                }}
              />
            )}
          </>
        )}
      </Modal>

      <ModalAddSkus
        isOpen={isAddSkusOpen}
        codigoGrade={detalhe?.codigoGrade ?? null}
        handleCancel={() => setIsAddSkusOpen(false)}
        onAdicionados={() => {
          setIsAddSkusOpen(false);
          onSkusAlterados();
        }}
      />
    </>
  );
};

export default ModalGradeDetails;

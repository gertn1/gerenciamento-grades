import React, { useEffect, useState } from 'react';
import { Button, Divider, Input, Modal, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import { fetchAddSkus, fetchSkusDisponiveis } from '../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../store';
import { clearSkusDisponiveis } from '../../store/grades/gradeDetailSlice';
import type { SkuResumo } from '../../store/grades/types';
import { alertError, alertSuccess, alertWarningList } from '../../utils/alerts';
import { getErrorMessage } from '../../utils/apiError';

const { Text } = Typography;

type Props = {
  isOpen: boolean;
  codigoGrade: number | null;
  handleCancel: () => void;
  onAdicionados: () => void;
};

// Busca e seleção ficam separadas: cada resultado da busca só entra na lista
// de rascunho (`selecionados`) quando o usuário clica em "+". Nada é enviado
// à API até o clique em "Confirmar" — o rascunho vive só neste componente.
const ModalAddSkus: React.FC<Props> = ({ isOpen, codigoGrade, handleCancel, onAdicionados }) => {
  const dispatch = useAppDispatch();
  const resultados = useSelector((state: RootState) => state.gradeDetail.skusDisponiveis);
  const buscando = useSelector((state: RootState) => state.gradeDetail.loadingSkusDisponiveis);
  const salvando = useSelector((state: RootState) => state.gradeDetail.updatingSkus);

  const [termo, setTermo] = useState('');
  const [selecionados, setSelecionados] = useState<SkuResumo[]>([]);

  useEffect(() => {
    if (!isOpen || codigoGrade === null || !termo.trim()) {
      dispatch(clearSkusDisponiveis());
      return;
    }

    const timer = setTimeout(() => {
      dispatch(fetchSkusDisponiveis({ codigoGrade, termo: termo.trim() }))
        .unwrap()
        .catch((error) => alertError(getErrorMessage(error, 'Não foi possível buscar SKUs.')));
    }, 350);

    return () => clearTimeout(timer);
  }, [isOpen, codigoGrade, termo, dispatch]);

  const handleAfterClose = () => {
    setTermo('');
    setSelecionados([]);
    dispatch(clearSkusDisponiveis());
  };

  const handleSelecionar = (sku: SkuResumo) => {
    setSelecionados((atual) => (atual.some((s) => s.codigoSku === sku.codigoSku) ? atual : [...atual, sku]));
  };

  const handleRemoverSelecionado = (codigoSku: string) => {
    setSelecionados((atual) => atual.filter((s) => s.codigoSku !== codigoSku));
  };

  const handleConfirmar = async () => {
    if (codigoGrade === null || selecionados.length === 0) return;

    try {
      const resultado = await dispatch(fetchAddSkus({ codigoGrade, skus: selecionados.map((s) => s.codigoSku) })).unwrap();
      const adicionados = selecionados.length - resultado.skusRejeitados.length;

      if (resultado.skusRejeitados.length > 0) {
        alertWarningList(
          `${resultado.skusRejeitados.length} SKU(s) não adicionado(s)`,
          resultado.skusRejeitados.map((rejeitado) => rejeitado.mensagem),
          adicionados > 0 ? `${adicionados} SKU(s) adicionado(s) à grade.` : undefined,
        );
      } else {
        alertSuccess(`${adicionados} SKU(s) adicionado(s) à grade.`);
      }

      onAdicionados();
    } catch (error) {
      alertError(getErrorMessage(error, 'Não foi possível adicionar os SKUs.'));
    }
  };

  const colunasResultados: ColumnsType<SkuResumo> = [
    { title: 'Código', dataIndex: 'codigoSku', width: 110 },
    { title: 'Descrição', dataIndex: 'descricao' },
    {
      title: '',
      width: 60,
      align: 'center',
      render: (_, sku) => (
        <Button
          shape="circle"
          size="small"
          icon={<PlusOutlined />}
          disabled={selecionados.some((s) => s.codigoSku === sku.codigoSku)}
          onClick={() => handleSelecionar(sku)}
        />
      ),
    },
  ];

  const colunasSelecionados: ColumnsType<SkuResumo> = [
    { title: 'Código', dataIndex: 'codigoSku', width: 110 },
    { title: 'Descrição', dataIndex: 'descricao' },
    {
      title: '',
      width: 60,
      align: 'center',
      render: (_, sku) => (
        <Button shape="circle" size="small" danger icon={<DeleteOutlined />} onClick={() => handleRemoverSelecionado(sku.codigoSku)} />
      ),
    },
  ];

  return (
    <Modal
      title="Adicionar SKUs"
      open={isOpen}
      onCancel={handleCancel}
      afterClose={handleAfterClose}
      onOk={handleConfirmar}
      confirmLoading={salvando}
      okText={`Confirmar${selecionados.length > 0 ? ` (${selecionados.length})` : ''}`}
      okButtonProps={{ disabled: selecionados.length === 0 }}
      cancelText="Cancelar"
      destroyOnClose
      width={640}
    >
      <Input
        placeholder="Buscar SKU por código..."
        prefix={<SearchOutlined />}
        value={termo}
        onChange={(e) => setTermo(e.target.value)}
        allowClear
        autoFocus
        style={{ marginBottom: 12 }}
      />

      <Table
        size="small"
        bordered
        columns={colunasResultados}
        dataSource={resultados}
        rowKey="codigoSku"
        loading={buscando}
        pagination={{ pageSize: 5, hideOnSinglePage: true }}
        scroll={{ y: 180 }}
        locale={{ emptyText: termo ? 'Nenhum SKU encontrado.' : 'Digite para buscar SKUs disponíveis.' }}
      />

      <Divider style={{ margin: '16px 0' }} />

      <Text strong>SKUs selecionados para adicionar ({selecionados.length})</Text>

      <Table
        style={{ marginTop: 8 }}
        size="small"
        bordered
        columns={colunasSelecionados}
        dataSource={selecionados}
        rowKey="codigoSku"
        pagination={{ pageSize: 5, hideOnSinglePage: true }}
        scroll={{ y: 180 }}
        locale={{ emptyText: 'Nenhum SKU selecionado ainda — use a busca acima.' }}
      />
    </Modal>
  );
};

export default ModalAddSkus;

import { SearchOutlined } from '@ant-design/icons';
import { Input, Modal, Table, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useState } from 'react';
import { adicionarSkus, buscarSkusDisponiveis, extrairMensagemErro } from '../api/gradesApi';
import type { SkuResumo } from '../types/grade';

interface AddSkusModalProps {
  open: boolean;
  gradeCodigo: number | null;
  onClose: () => void;
  onAdicionados: () => void;
}

export function AddSkusModal({ open, gradeCodigo, onClose, onAdicionados }: AddSkusModalProps) {
  const [termo, setTermo] = useState('');
  const [resultados, setResultados] = useState<SkuResumo[]>([]);
  const [buscando, setBuscando] = useState(false);
  const [selecionados, setSelecionados] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!open) return;
    setTermo('');
    setResultados([]);
    setSelecionados([]);
  }, [open]);

  useEffect(() => {
    if (!open || !gradeCodigo || !termo.trim()) {
      setResultados([]);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        setBuscando(true);
        const dados = await buscarSkusDisponiveis(gradeCodigo, termo);
        setResultados(dados);
      } catch (error) {
        message.error(extrairMensagemErro(error, 'Não foi possível buscar SKUs.'));
      } finally {
        setBuscando(false);
      }
    }, 350);

    return () => clearTimeout(timer);
  }, [open, gradeCodigo, termo]);

  async function handleAdicionar() {
    if (!gradeCodigo || selecionados.length === 0) return;

    try {
      setSalvando(true);
      const resultado = await adicionarSkus(gradeCodigo, selecionados);

      if (resultado.skusRejeitados.length > 0) {
        Modal.warning({
          title: `${resultado.skusRejeitados.length} SKU(s) não adicionado(s)`,
          content: (
            <ul style={{ paddingLeft: 20, margin: 0 }}>
              {resultado.skusRejeitados.map((rejeitado) => (
                <li key={rejeitado.sku}>{rejeitado.mensagem}</li>
              ))}
            </ul>
          ),
        });
      }

      const adicionados = selecionados.length - resultado.skusRejeitados.length;
      if (adicionados > 0) {
        message.success(`${adicionados} SKU(s) adicionado(s) à grade.`);
      }

      onAdicionados();
    } catch (error) {
      message.error(extrairMensagemErro(error, 'Não foi possível adicionar os SKUs.'));
    } finally {
      setSalvando(false);
    }
  }

  const columns: ColumnsType<SkuResumo> = [
    { title: 'CÓDIGO', dataIndex: 'codigo', width: 110 },
    { title: 'DESCRIÇÃO', dataIndex: 'descricao' },
  ];

  return (
    <Modal
      title="Adicionar SKUs"
      open={open}
      onCancel={onClose}
      onOk={handleAdicionar}
      confirmLoading={salvando}
      okText={`Adicionar${selecionados.length > 0 ? ` (${selecionados.length})` : ''}`}
      okButtonProps={{ disabled: selecionados.length === 0 }}
      cancelText="Cancelar"
      destroyOnHidden
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
        columns={columns}
        dataSource={resultados}
        rowKey="codigo"
        loading={buscando}
        pagination={{ pageSize: 8, hideOnSinglePage: true }}
        scroll={{ y: 320 }}
        rowSelection={{
          selectedRowKeys: selecionados,
          onChange: (keys) => setSelecionados(keys as string[]),
        }}
        locale={{ emptyText: termo ? 'Nenhum SKU encontrado.' : 'Digite para buscar SKUs disponíveis.' }}
      />
    </Modal>
  );
}

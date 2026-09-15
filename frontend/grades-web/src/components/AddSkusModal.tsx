import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { Button, Divider, Input, Modal, Table, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useState } from 'react';
import { adicionarSkus, buscarSkusDisponiveis, extrairMensagemErro } from '../api/gradesApi';
import type { SkuResumo } from '../types/grade';

const { Text } = Typography;

interface AddSkusModalProps {
  open: boolean;
  gradeCodigo: number | null;
  onClose: () => void;
  onAdicionados: () => void;
}

// Busca e seleção ficam separadas: cada resultado da busca só entra na lista
// de rascunho (`selecionados`) quando o usuário clica em "+". Nada é enviado
// à API até o clique em "Confirmar" — o rascunho vive só em memória, e cada
// item tem seu próprio botão de remover pra desfazer uma adição por engano.
export function AddSkusModal({ open, gradeCodigo, onClose, onAdicionados }: AddSkusModalProps) {
  const [termo, setTermo] = useState('');
  const [resultados, setResultados] = useState<SkuResumo[]>([]);
  const [buscando, setBuscando] = useState(false);
  const [selecionados, setSelecionados] = useState<SkuResumo[]>([]);
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

  function handleSelecionar(sku: SkuResumo) {
    setSelecionados((atual) => (atual.some((s) => s.codigoSku === sku.codigoSku) ? atual : [...atual, sku]));
  }

  function handleRemoverSelecionado(codigoSku: string) {
    setSelecionados((atual) => atual.filter((s) => s.codigoSku !== codigoSku));
  }

  async function handleConfirmar() {
    if (!gradeCodigo || selecionados.length === 0) return;

    try {
      setSalvando(true);
      const resultado = await adicionarSkus(
        gradeCodigo,
        selecionados.map((s) => s.codigoSku),
      );

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

  const colunasResultados: ColumnsType<SkuResumo> = [
    { title: 'CÓDIGO', dataIndex: 'codigoSku', width: 110 },
    { title: 'DESCRIÇÃO', dataIndex: 'descricao' },
    {
      title: '',
      width: 48,
      align: 'center',
      render: (_, sku) => {
        const jaSelecionado = selecionados.some((s) => s.codigoSku === sku.codigoSku);
        return (
          <Button
            type="text"
            size="small"
            icon={<PlusOutlined />}
            disabled={jaSelecionado}
            onClick={() => handleSelecionar(sku)}
          />
        );
      },
    },
  ];

  const colunasSelecionados: ColumnsType<SkuResumo> = [
    { title: 'CÓDIGO', dataIndex: 'codigoSku', width: 110 },
    { title: 'DESCRIÇÃO', dataIndex: 'descricao' },
    {
      title: '',
      width: 48,
      align: 'center',
      render: (_, sku) => (
        <Button
          type="text"
          danger
          size="small"
          icon={<DeleteOutlined />}
          onClick={() => handleRemoverSelecionado(sku.codigoSku)}
        />
      ),
    },
  ];

  return (
    <Modal
      title="Adicionar SKUs"
      open={open}
      onCancel={onClose}
      onOk={handleConfirmar}
      confirmLoading={salvando}
      okText={`Confirmar${selecionados.length > 0 ? ` (${selecionados.length})` : ''}`}
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
        columns={colunasSelecionados}
        dataSource={selecionados}
        rowKey="codigoSku"
        pagination={{ pageSize: 5, hideOnSinglePage: true }}
        scroll={{ y: 180 }}
        locale={{ emptyText: 'Nenhum SKU selecionado ainda — use a busca acima.' }}
      />
    </Modal>
  );
}

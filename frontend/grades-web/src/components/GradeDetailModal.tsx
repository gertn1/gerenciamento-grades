import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { Button, Empty, Input, Modal, Skeleton, Space, Table, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useMemo, useState } from 'react';
import { extrairMensagemErro, obterDetalheGrade, removerSkus } from '../api/gradesApi';
import type { GradeDetalhe, SkuResumo } from '../types/grade';
import { AddSkusModal } from './AddSkusModal';

const { Text } = Typography;

interface GradeDetailModalProps {
  open: boolean;
  codigo: number | null;
  refreshKey?: number;
  onClose: () => void;
  onEditarGrade: (grade: GradeDetalhe) => void;
  onExcluirGrade: (grade: GradeDetalhe) => void;
}

export function GradeDetailModal({
  open,
  codigo,
  refreshKey,
  onClose,
  onEditarGrade,
  onExcluirGrade,
}: GradeDetailModalProps) {
  const [carregando, setCarregando] = useState(false);
  const [detalhe, setDetalhe] = useState<GradeDetalhe | null>(null);
  const [filtro, setFiltro] = useState('');
  const [selecionados, setSelecionados] = useState<string[]>([]);
  const [removendo, setRemovendo] = useState(false);
  const [adicionarAberto, setAdicionarAberto] = useState(false);

  const carregarDetalhe = useMemo(
    () => async (codigoGrade: number) => {
      setCarregando(true);
      try {
        const dados = await obterDetalheGrade(codigoGrade);
        setDetalhe(dados);
      } catch (error) {
        message.error(extrairMensagemErro(error, 'Não foi possível carregar a grade.'));
      } finally {
        setCarregando(false);
      }
    },
    [],
  );

  useEffect(() => {
    if (!open || !codigo) return;

    setFiltro('');
    setSelecionados([]);
    setDetalhe(null);
    carregarDetalhe(codigo);
  }, [open, codigo, refreshKey, carregarDetalhe]);

  async function handleRemover(skus: string[]) {
    if (!detalhe || skus.length === 0) return;

    try {
      setRemovendo(true);
      const resultado = await removerSkus(detalhe.codigoGrade, skus);
      setDetalhe(resultado.grade);
      setSelecionados((atual) => atual.filter((sku) => !skus.includes(sku)));
      message.success(`${skus.length} SKU(s) removido(s) da grade.`);
    } catch (error) {
      message.error(extrairMensagemErro(error, 'Não foi possível remover os SKUs.'));
    } finally {
      setRemovendo(false);
    }
  }

  function confirmarRemocao(skus: string[]) {
    Modal.confirm({
      title: skus.length === 1 ? 'Remover SKU da grade?' : `Remover ${skus.length} SKUs da grade?`,
      content: 'O(s) produto(s) removido(s) ficará(ão) sem grade vinculada.',
      okText: 'Remover',
      okButtonProps: { danger: true },
      cancelText: 'Cancelar',
      onOk: () => handleRemover(skus),
    });
  }

  const skusFiltrados = useMemo(() => {
    if (!detalhe) return [];
    const termo = filtro.trim().toLowerCase();
    if (!termo) return detalhe.skus;
    return detalhe.skus.filter(
      (sku) => sku.codigoSku.toLowerCase().includes(termo) || sku.descricao.toLowerCase().includes(termo),
    );
  }, [detalhe, filtro]);

  const columns: ColumnsType<SkuResumo> = [
    {
      title: 'CÓDIGO',
      dataIndex: 'codigoSku',
      width: 110,
      render: (codigo: string) => <Text code>{codigo}</Text>,
    },
    { title: 'DESCRIÇÃO', dataIndex: 'descricao' },
    {
      title: '',
      width: 48,
      align: 'center',
      render: (_, sku) => (
        <Button type="text" danger size="small" icon={<DeleteOutlined />} onClick={() => confirmarRemocao([sku.codigoSku])} />
      ),
    },
  ];

  return (
    <>
      <Modal
        title={detalhe ? detalhe.nome : 'SKUs vinculados'}
        open={open}
        onCancel={onClose}
        width={760}
        destroyOnHidden
        footer={
          detalhe && (
            <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button icon={<EditOutlined />} onClick={() => onEditarGrade(detalhe)}>
                Editar grade
              </Button>
              <Button danger icon={<DeleteOutlined />} onClick={() => onExcluirGrade(detalhe)}>
                Excluir grade
              </Button>
              <Button type="primary" onClick={onClose} style={{ background: '#0b3d63' }}>
                Fechar
              </Button>
            </Space>
          )
        }
      >
        {carregando && <Skeleton active />}

        {!carregando && detalhe && (
          <>
            <Text type="secondary">
              Código {detalhe.codigoGrade} · Sigla: {detalhe.sigla}
            </Text>

            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 20, marginBottom: 8 }}>
              <Text strong>SKUS VINCULADOS</Text>
              <Space>
                <Button
                  danger
                  disabled={selecionados.length === 0}
                  onClick={() => confirmarRemocao(selecionados)}
                >
                  Remover selecionados
                </Button>
                <Button type="primary" icon={<PlusOutlined />} onClick={() => setAdicionarAberto(true)} style={{ background: '#0b3d63' }}>
                  Adicionar SKUs
                </Button>
              </Space>
            </div>

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
                columns={columns}
                dataSource={skusFiltrados}
                rowKey="codigoSku"
                loading={removendo}
                pagination={{ pageSize: 10, hideOnSinglePage: true }}
                rowSelection={{
                  selectedRowKeys: selecionados,
                  onChange: (keys) => setSelecionados(keys as string[]),
                }}
              />
            )}
          </>
        )}
      </Modal>

      <AddSkusModal
        open={adicionarAberto}
        gradeCodigo={detalhe?.codigoGrade ?? null}
        onClose={() => setAdicionarAberto(false)}
        onAdicionados={() => {
          setAdicionarAberto(false);
          if (codigo) carregarDetalhe(codigo);
        }}
      />
    </>
  );
}

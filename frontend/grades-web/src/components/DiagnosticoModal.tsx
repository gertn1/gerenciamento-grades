import { Button, Modal, Table, Tabs, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useState } from 'react';
import { extrairMensagemErro, listarGradesVazias, listarSkusOrfaos } from '../api/gradesApi';
import type { GradeListItem, SkuResumo } from '../types/grade';

const { Text } = Typography;

const TAMANHO_PAGINA_ORFAOS = 10;

interface DiagnosticoModalProps {
  open: boolean;
  onClose: () => void;
  onAbrirGrade: (codigo: number) => void;
}

export function DiagnosticoModal({ open, onClose, onAbrirGrade }: DiagnosticoModalProps) {
  const [skusOrfaos, setSkusOrfaos] = useState<SkuResumo[]>([]);
  const [totalOrfaos, setTotalOrfaos] = useState(0);
  const [paginaOrfaos, setPaginaOrfaos] = useState(1);
  const [carregandoOrfaos, setCarregandoOrfaos] = useState(false);

  const [gradesVazias, setGradesVazias] = useState<GradeListItem[]>([]);
  const [carregandoVazias, setCarregandoVazias] = useState(false);

  useEffect(() => {
    if (open) setPaginaOrfaos(1);
  }, [open]);

  useEffect(() => {
    if (!open) return;

    (async () => {
      try {
        setCarregandoOrfaos(true);
        const dados = await listarSkusOrfaos(paginaOrfaos, TAMANHO_PAGINA_ORFAOS);
        setSkusOrfaos(dados.itens);
        setTotalOrfaos(dados.total);
      } catch (error) {
        message.error(extrairMensagemErro(error, 'Não foi possível carregar os SKUs órfãos.'));
      } finally {
        setCarregandoOrfaos(false);
      }
    })();
  }, [open, paginaOrfaos]);

  useEffect(() => {
    if (!open) return;

    (async () => {
      try {
        setCarregandoVazias(true);
        const dados = await listarGradesVazias();
        setGradesVazias(dados);
      } catch (error) {
        message.error(extrairMensagemErro(error, 'Não foi possível carregar as grades vazias.'));
      } finally {
        setCarregandoVazias(false);
      }
    })();
  }, [open]);

  function handleAbrirGrade(codigo: number) {
    onAbrirGrade(codigo);
    onClose();
  }

  const colunasOrfaos: ColumnsType<SkuResumo> = [
    { title: 'CÓDIGO', dataIndex: 'codigo', width: 110 },
    { title: 'DESCRIÇÃO', dataIndex: 'descricao' },
  ];

  const colunasVazias: ColumnsType<GradeListItem> = [
    { title: 'CÓDIGO', dataIndex: 'codigo', width: 90 },
    { title: 'NOME', dataIndex: 'nome' },
    { title: 'SIGLA', dataIndex: 'sigla', width: 140 },
    {
      title: '',
      width: 80,
      align: 'center',
      render: (_, grade) => (
        <Button type="link" size="small" onClick={() => handleAbrirGrade(grade.codigo)}>
          Abrir
        </Button>
      ),
    },
  ];

  return (
    <Modal title="SKUs órfãos e grades vazias" open={open} onCancel={onClose} footer={null} width={720} destroyOnHidden>
      <Tabs
        items={[
          {
            key: 'orfaos',
            label: `SKUs órfãos (${totalOrfaos})`,
            children: (
              <>
                <Text type="secondary">Produtos sem nenhuma grade vinculada.</Text>
                <Table
                  style={{ marginTop: 12 }}
                  size="small"
                  columns={colunasOrfaos}
                  dataSource={skusOrfaos}
                  rowKey="codigo"
                  loading={carregandoOrfaos}
                  pagination={{
                    current: paginaOrfaos,
                    pageSize: TAMANHO_PAGINA_ORFAOS,
                    total: totalOrfaos,
                    onChange: setPaginaOrfaos,
                    showSizeChanger: false,
                  }}
                />
              </>
            ),
          },
          {
            key: 'vazias',
            label: `Grades vazias (${gradesVazias.length})`,
            children: (
              <>
                <Text type="secondary">Grades cadastradas sem nenhum SKU vinculado.</Text>
                <Table
                  style={{ marginTop: 12 }}
                  size="small"
                  columns={colunasVazias}
                  dataSource={gradesVazias}
                  rowKey="codigo"
                  loading={carregandoVazias}
                  pagination={{ pageSize: 10, hideOnSinglePage: true }}
                />
              </>
            ),
          },
        ]}
      />
    </Modal>
  );
}

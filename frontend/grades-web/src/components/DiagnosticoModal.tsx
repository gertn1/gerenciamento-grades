import { DownloadOutlined, SearchOutlined } from '@ant-design/icons';
import { Button, Col, Input, InputNumber, Modal, Row, Table, Tabs, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useState } from 'react';
import { extrairMensagemErro, listarGradesVazias, listarSkusOrfaos, urlExportarSkusOrfaos } from '../api/gradesApi';
import type { GradeListItem, SkuResumo } from '../types/grade';

const { Text } = Typography;

const TAMANHO_PAGINA_ORFAOS = 10;

interface DiagnosticoModalProps {
  open: boolean;
  onClose: () => void;
  onAbrirGrade: (codigoGrade: number) => void;
}

export function DiagnosticoModal({ open, onClose, onAbrirGrade }: DiagnosticoModalProps) {
  const [termoOrfaos, setTermoOrfaos] = useState('');
  const [skusOrfaos, setSkusOrfaos] = useState<SkuResumo[]>([]);
  const [totalOrfaos, setTotalOrfaos] = useState(0);
  const [paginaOrfaos, setPaginaOrfaos] = useState(1);
  const [carregandoOrfaos, setCarregandoOrfaos] = useState(false);

  const [codigoGradeVazias, setCodigoGradeVazias] = useState<number | null>(null);
  const [nomeVazias, setNomeVazias] = useState('');
  const [gradesVazias, setGradesVazias] = useState<GradeListItem[]>([]);
  const [carregandoVazias, setCarregandoVazias] = useState(false);

  useEffect(() => {
    if (!open) return;
    setTermoOrfaos('');
    setPaginaOrfaos(1);
    setCodigoGradeVazias(null);
    setNomeVazias('');
  }, [open]);

  useEffect(() => {
    if (!open) return;

    const timer = setTimeout(async () => {
      try {
        setCarregandoOrfaos(true);
        const dados = await listarSkusOrfaos(paginaOrfaos, TAMANHO_PAGINA_ORFAOS, termoOrfaos || undefined);
        setSkusOrfaos(dados.itens);
        setTotalOrfaos(dados.total);
      } catch (error) {
        message.error(extrairMensagemErro(error, 'Não foi possível carregar os SKUs órfãos.'));
      } finally {
        setCarregandoOrfaos(false);
      }
    }, 350);

    return () => clearTimeout(timer);
  }, [open, paginaOrfaos, termoOrfaos]);

  useEffect(() => {
    if (!open) return;

    const timer = setTimeout(async () => {
      try {
        setCarregandoVazias(true);
        const dados = await listarGradesVazias({
          codigoGrade: codigoGradeVazias ?? undefined,
          nome: nomeVazias || undefined,
        });
        setGradesVazias(dados);
      } catch (error) {
        message.error(extrairMensagemErro(error, 'Não foi possível carregar as grades vazias.'));
      } finally {
        setCarregandoVazias(false);
      }
    }, 350);

    return () => clearTimeout(timer);
  }, [open, codigoGradeVazias, nomeVazias]);

  function handleAbrirGrade(codigoGrade: number) {
    onAbrirGrade(codigoGrade);
    onClose();
  }

  const colunasOrfaos: ColumnsType<SkuResumo> = [
    { title: 'CÓDIGO', dataIndex: 'codigoSku', width: 110 },
    { title: 'DESCRIÇÃO', dataIndex: 'descricao' },
  ];

  const colunasVazias: ColumnsType<GradeListItem> = [
    { title: 'CÓDIGO', dataIndex: 'codigoGrade', width: 90 },
    { title: 'NOME', dataIndex: 'nome' },
    { title: 'SIGLA', dataIndex: 'sigla', width: 140 },
    {
      title: '',
      width: 80,
      align: 'center',
      render: (_, grade) => (
        <Button type="link" size="small" onClick={() => handleAbrirGrade(grade.codigoGrade)}>
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
                <Row gutter={12} style={{ marginTop: 12 }}>
                  <Col flex="1 1 auto">
                    <Input
                      placeholder="Buscar por código do SKU..."
                      prefix={<SearchOutlined />}
                      value={termoOrfaos}
                      onChange={(e) => {
                        setTermoOrfaos(e.target.value);
                        setPaginaOrfaos(1);
                      }}
                      allowClear
                    />
                  </Col>
                  <Col>
                    <Button
                      icon={<DownloadOutlined />}
                      href={urlExportarSkusOrfaos(termoOrfaos.trim() || undefined)}
                      disabled={totalOrfaos === 0}
                    >
                      Exportar CSV
                    </Button>
                  </Col>
                </Row>
                <Table
                  style={{ marginTop: 12 }}
                  size="small"
                  columns={colunasOrfaos}
                  dataSource={skusOrfaos}
                  rowKey="codigoSku"
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
                <Row gutter={12} style={{ marginTop: 12 }}>
                  <Col>
                    <InputNumber
                      placeholder="Código"
                      value={codigoGradeVazias}
                      onChange={setCodigoGradeVazias}
                      style={{ width: 120 }}
                    />
                  </Col>
                  <Col flex="1 1 auto">
                    <Input
                      placeholder="Buscar por nome da grade..."
                      prefix={<SearchOutlined />}
                      value={nomeVazias}
                      onChange={(e) => setNomeVazias(e.target.value)}
                      allowClear
                    />
                  </Col>
                </Row>
                <Table
                  style={{ marginTop: 12 }}
                  size="small"
                  columns={colunasVazias}
                  dataSource={gradesVazias}
                  rowKey="codigoGrade"
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

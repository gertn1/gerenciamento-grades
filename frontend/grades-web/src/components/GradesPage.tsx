import {
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  FileExcelOutlined,
  FileSearchOutlined,
  ImportOutlined,
  PlusOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { Button, Card, Col, Input, InputNumber, Row, Space, Table, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useCallback, useEffect, useState } from 'react';
import {
  criacaoMassiva,
  exclusaoMassivaSkus,
  extrairMensagemErro,
  importacaoMassivaAtualizacao,
  listarGrades,
  urlModeloCriacaoMassiva,
  urlModeloExclusaoMassivaSkus,
  urlModeloImportacaoMassivaAtualizacao,
} from '../api/gradesApi';
import type { Grade, GradeListItem } from '../types/grade';
import { AppHeader } from './AppHeader';
import { BulkOperationModal } from './BulkOperationModal';
import { DiagnosticoModal } from './DiagnosticoModal';
import { GradeDeleteModal } from './GradeDeleteModal';
import { GradeDetailModal } from './GradeDetailModal';
import { GradeFormModal } from './GradeFormModal';

const { Title, Text } = Typography;

export function GradesPage() {
  const [grades, setGrades] = useState<GradeListItem[]>([]);
  const [carregando, setCarregando] = useState(false);
  const [filtroCodigo, setFiltroCodigo] = useState<number | null>(null);
  const [filtroNome, setFiltroNome] = useState('');

  const [formAberto, setFormAberto] = useState(false);
  const [gradeEmEdicao, setGradeEmEdicao] = useState<Grade | null>(null);

  const [detalheAberto, setDetalheAberto] = useState(false);
  const [codigoDetalhe, setCodigoDetalhe] = useState<number | null>(null);
  const [detalheRefreshKey, setDetalheRefreshKey] = useState(0);

  const [exclusaoAberta, setExclusaoAberta] = useState(false);
  const [gradeParaExcluir, setGradeParaExcluir] = useState<{ codigoGrade: number; nome: string; qtdSkus: number } | null>(
    null,
  );

  const [criacaoMassivaAberta, setCriacaoMassivaAberta] = useState(false);
  const [importacaoMassivaAberta, setImportacaoMassivaAberta] = useState(false);
  const [exclusaoMassivaAberta, setExclusaoMassivaAberta] = useState(false);
  const [diagnosticoAberto, setDiagnosticoAberto] = useState(false);

  const carregarGrades = useCallback(async () => {
    try {
      setCarregando(true);
      const dados = await listarGrades({
        codigoGrade: filtroCodigo ?? undefined,
        nome: filtroNome || undefined,
      });
      setGrades(dados);
    } catch (error) {
      message.error(extrairMensagemErro(error, 'Não foi possível carregar as grades.'));
    } finally {
      setCarregando(false);
    }
  }, [filtroCodigo, filtroNome]);

  useEffect(() => {
    carregarGrades();
  }, [carregarGrades]);

  const columns: ColumnsType<GradeListItem> = [
    {
      title: 'CÓDIGO',
      dataIndex: 'codigoGrade',
      width: 110,
      render: (codigo: number) => (
        <span
          style={{
            background: '#e6f4ff',
            color: '#0958d9',
            borderRadius: 4,
            padding: '2px 8px',
            fontWeight: 600,
          }}
        >
          {codigo}
        </span>
      ),
    },
    { title: 'NOME', dataIndex: 'nome' },
    { title: 'SIGLA', dataIndex: 'sigla', render: (sigla: string) => <Text code>{sigla}</Text> },
    {
      title: 'SKUS',
      dataIndex: 'qtdSkus',
      align: 'center',
      width: 90,
      render: (qtdSkus: number) => <Text strong={qtdSkus > 0}>{qtdSkus}</Text>,
    },
    {
      title: 'AÇÕES',
      align: 'center',
      width: 130,
      render: (_, grade) => (
        <Space>
          <Button
            type="text"
            icon={<EyeOutlined />}
            onClick={() => {
              setCodigoDetalhe(grade.codigoGrade);
              setDetalheAberto(true);
            }}
          />
          <Button
            type="text"
            icon={<EditOutlined />}
            onClick={() => {
              setGradeEmEdicao(grade);
              setFormAberto(true);
            }}
          />
          <Button
            type="text"
            danger
            icon={<DeleteOutlined />}
            onClick={() => {
              setGradeParaExcluir(grade);
              setExclusaoAberta(true);
            }}
          />
        </Space>
      ),
    },
  ];

  return (
    <>
      <AppHeader />

      <div style={{ maxWidth: 1100, margin: '0 auto', padding: 24 }}>
        <Title level={3} style={{ color: '#0b3d63', marginBottom: 0 }}>
          Gerenciamento de Grades
        </Title>
        <Text type="secondary">Gestão de famílias de SKUs para precificação</Text>

        <Row gutter={12} style={{ marginTop: 24, marginBottom: 16 }} align="middle">
          <Col>
            <InputNumber
              placeholder="Código"
              value={filtroCodigo}
              onChange={(valor) => setFiltroCodigo(valor)}
              style={{ width: 120 }}
            />
          </Col>
          <Col flex="1 1 260px">
            <Input.Search
              placeholder="Buscar por nome da grade..."
              value={filtroNome}
              onChange={(e) => setFiltroNome(e.target.value)}
              onSearch={carregarGrades}
              allowClear
            />
          </Col>
          <Col>
            <Button icon={<FileExcelOutlined />} onClick={() => setCriacaoMassivaAberta(true)}>
              Criação massiva de grades
            </Button>
          </Col>
          <Col>
            <Button icon={<ImportOutlined />} onClick={() => setImportacaoMassivaAberta(true)}>
              Importação massiva
            </Button>
          </Col>
          <Col>
            <Button icon={<UploadOutlined />} onClick={() => setExclusaoMassivaAberta(true)}>
              Exclusão massiva
            </Button>
          </Col>
          <Col>
            <Button icon={<FileSearchOutlined />} onClick={() => setDiagnosticoAberto(true)}>
              Diagnóstico
            </Button>
          </Col>
          <Col>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                setGradeEmEdicao(null);
                setFormAberto(true);
              }}
              style={{ background: '#0b3d63' }}
            >
              Nova grade
            </Button>
          </Col>
        </Row>

        <Card>
          <Text type="secondary">{grades.length} grade(s) encontrada(s)</Text>
          <Table
            style={{ marginTop: 12 }}
            columns={columns}
            dataSource={grades}
            rowKey="codigoGrade"
            loading={carregando}
            pagination={{ pageSize: 10, hideOnSinglePage: true }}
          />
        </Card>
      </div>

      <GradeFormModal
        open={formAberto}
        grade={gradeEmEdicao}
        onClose={() => setFormAberto(false)}
        onSaved={() => {
          setFormAberto(false);
          carregarGrades();
          if (detalheAberto) setDetalheRefreshKey((chave) => chave + 1);
        }}
      />

      <GradeDetailModal
        open={detalheAberto}
        codigo={codigoDetalhe}
        refreshKey={detalheRefreshKey}
        onClose={() => setDetalheAberto(false)}
        onEditarGrade={(grade) => {
          setGradeEmEdicao(grade);
          setFormAberto(true);
        }}
        onExcluirGrade={(grade) => {
          setGradeParaExcluir({ codigoGrade: grade.codigoGrade, nome: grade.nome, qtdSkus: grade.skus.length });
          setExclusaoAberta(true);
        }}
      />

      <GradeDeleteModal
        open={exclusaoAberta}
        grade={gradeParaExcluir}
        onClose={() => setExclusaoAberta(false)}
        onDeleted={() => {
          setExclusaoAberta(false);
          setDetalheAberto(false);
          carregarGrades();
        }}
      />

      <BulkOperationModal
        open={criacaoMassivaAberta}
        title="Criação massiva de grades"
        subtitle="Crie grades e vincule SKUs de uma vez via arquivo Excel."
        warningText="O código da grade é gerado de forma automática pelo sistema. Preencha somente as colunas informadas no modelo da planilha."
        templateUrl={urlModeloCriacaoMassiva()}
        templateFileName="modelo_criacao_massiva_grades.xlsx"
        onUpload={criacaoMassiva}
        onFinished={carregarGrades}
        onClose={() => setCriacaoMassivaAberta(false)}
      />

      <BulkOperationModal
        open={importacaoMassivaAberta}
        title="Importação massiva"
        subtitle="Atualize o vínculo de SKUs em grades já existentes via arquivo Excel — não cria grades novas."
        warningText="Preencha CODIGO_GRADE com o código de uma grade já existente e CODIGO_SKU com o código do produto."
        templateUrl={urlModeloImportacaoMassivaAtualizacao()}
        templateFileName="modelo_importacao_massiva_atualizacao.xlsx"
        onUpload={importacaoMassivaAtualizacao}
        onFinished={carregarGrades}
        onClose={() => setImportacaoMassivaAberta(false)}
      />

      <DiagnosticoModal
        open={diagnosticoAberto}
        onClose={() => setDiagnosticoAberto(false)}
        onAbrirGrade={(codigo) => {
          setCodigoDetalhe(codigo);
          setDetalheAberto(true);
        }}
      />

      <BulkOperationModal
        open={exclusaoMassivaAberta}
        title="Exclusão massiva de SKUs"
        subtitle="Remova SKUs de múltiplas grades via arquivo Excel."
        warningText="A grade não será excluída: ela ficará vazia e os SKUs serão desvinculados."
        templateUrl={urlModeloExclusaoMassivaSkus()}
        templateFileName="modelo_exclusao_massiva_skus.xlsx"
        onUpload={exclusaoMassivaSkus}
        onFinished={carregarGrades}
        onClose={() => setExclusaoMassivaAberta(false)}
        danger
      />
    </>
  );
}

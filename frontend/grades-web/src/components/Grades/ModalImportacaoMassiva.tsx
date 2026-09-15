import React, { useState } from 'react';
import { Alert, Button, List, Modal, Row, Tag, Typography, Upload } from 'antd';
import type { UploadFile } from 'antd';
import { CloudDownloadOutlined, InboxOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import { fetchImportacaoMassiva, urlModeloImportacao } from '../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../store';
import { clearImportacao } from '../../store/grades/importacaoSlice';
import type { TipoImportacaoMassiva } from '../../store/grades/types';
import { alertError, alertSuccess } from '../../utils/alerts';
import { getErrorMessage } from '../../utils/apiError';

const { Dragger } = Upload;
const { Paragraph, Text } = Typography;

interface ConfiguracaoImportacao {
  title: string;
  subtitle: string;
  warningText: string;
  templateFileName: string;
  danger?: boolean;
}

const CONFIGURACAO: Record<TipoImportacaoMassiva, ConfiguracaoImportacao> = {
  criacao: {
    title: 'Criação Massiva de Grades',
    subtitle: 'Crie grades e vincule SKUs de uma vez via arquivo Excel.',
    warningText: 'O código da grade é gerado de forma automática pelo sistema. Preencha somente as colunas informadas no modelo da planilha.',
    templateFileName: 'modelo_criacao_massiva_grades.xlsx',
  },
  atualizacao: {
    title: 'Importação Massiva',
    subtitle: 'Atualize o vínculo de SKUs em grades já existentes via arquivo Excel — não cria grades novas.',
    warningText: 'Preencha CODIGO_GRADE com o código de uma grade já existente e CODIGO_SKU com o código do produto.',
    templateFileName: 'modelo_importacao_massiva_atualizacao.xlsx',
  },
  exclusao: {
    title: 'Exclusão Massiva de SKUs',
    subtitle: 'Remova SKUs de múltiplas grades via arquivo Excel.',
    warningText: 'A grade não será excluída: ela ficará vazia e os SKUs serão desvinculados.',
    templateFileName: 'modelo_exclusao_massiva_skus.xlsx',
    danger: true,
  },
};

type Props = {
  isOpen: boolean;
  tipo: TipoImportacaoMassiva;
  handleCancel: () => void;
  onFinished: () => void;
};

const ModalImportacaoMassiva: React.FC<Props> = ({ isOpen, tipo, handleCancel, onFinished }) => {
  const dispatch = useAppDispatch();
  const resultado = useSelector((state: RootState) => state.importacao.data);
  const loading = useSelector((state: RootState) => state.importacao.loading);
  const [arquivo, setArquivo] = useState<File | null>(null);

  const configuracao = CONFIGURACAO[tipo];
  const fileList: UploadFile[] = arquivo ? [{ uid: '-1', name: arquivo.name, status: 'done' }] : [];

  const handleAfterClose = () => {
    setArquivo(null);
    dispatch(clearImportacao());
  };

  const handleUpload = async () => {
    if (!arquivo) return;

    try {
      const result = await dispatch(fetchImportacaoMassiva({ tipo, arquivo })).unwrap();
      if (result.erros.length === 0) {
        alertSuccess('Arquivo processado com sucesso.');
      }
      onFinished();
    } catch (error) {
      alertError(getErrorMessage(error, 'Não foi possível processar o arquivo.'));
    }
  };

  return (
    <Modal title={configuracao.title} open={isOpen} onCancel={handleCancel} afterClose={handleAfterClose} footer={false} width={560} destroyOnClose>
      <Paragraph type="secondary">{configuracao.subtitle}</Paragraph>
      <Alert type="warning" showIcon message={configuracao.warningText} />

      <Row justify="end" style={{ marginTop: 25, marginBottom: 25 }}>
        <Button icon={<CloudDownloadOutlined />} type="primary" href={urlModeloImportacao(tipo)} download={configuracao.templateFileName}>
          Download Modelo de Planilha
        </Button>
      </Row>

      <Dragger
        accept=".xlsx"
        maxCount={1}
        fileList={fileList}
        beforeUpload={(file) => {
          setArquivo(file);
          dispatch(clearImportacao());
          return false;
        }}
        onRemove={() => {
          setArquivo(null);
          dispatch(clearImportacao());
        }}
      >
        <p className="ant-upload-drag-icon">
          <InboxOutlined />
        </p>
        <p className="ant-upload-text">Clique ou arraste um arquivo aqui para enviar</p>
        <p className="ant-upload-hint">Verifique se o arquivo .xlsx está no padrão do modelo antes de prosseguir.</p>
      </Dragger>

      {resultado && (
        <div style={{ marginTop: 16 }}>
          <Alert
            type={resultado.erros.length === 0 ? 'success' : 'warning'}
            showIcon
            message={
              <>
                Total de linhas: <Tag>{resultado.totalLinhas}</Tag>
                Sucesso: <Tag color="green">{resultado.sucesso}</Tag>
                Erros: <Tag color={resultado.erros.length > 0 ? 'red' : 'default'}>{resultado.erros.length}</Tag>
              </>
            }
          />

          {resultado.erros.length > 0 && (
            <List
              size="small"
              bordered
              style={{ marginTop: 8, maxHeight: 200, overflowY: 'auto' }}
              dataSource={resultado.erros}
              renderItem={(erro) => (
                <List.Item>
                  <Text type="danger">Linha {erro.linha}:</Text>&nbsp;{erro.mensagem}
                </List.Item>
              )}
            />
          )}
        </div>
      )}

      <Row justify="end" style={{ gap: 8, marginTop: 24 }}>
        <Button onClick={handleCancel} disabled={loading}>
          Fechar
        </Button>
        <Button type="primary" danger={configuracao.danger} onClick={handleUpload} loading={loading} disabled={!arquivo}>
          Processar Arquivo
        </Button>
      </Row>
    </Modal>
  );
};

export default ModalImportacaoMassiva;

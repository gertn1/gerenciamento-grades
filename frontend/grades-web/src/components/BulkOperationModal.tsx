import { DownloadOutlined, InboxOutlined } from '@ant-design/icons';
import { Alert, Button, List, Modal, Tag, Typography, Upload, message } from 'antd';
import type { UploadFile } from 'antd';
import { useState } from 'react';
import { extrairMensagemErro } from '../api/gradesApi';
import type { ImportacaoResult } from '../types/grade';

const { Text, Paragraph } = Typography;

interface BulkOperationModalProps {
  open: boolean;
  title: string;
  subtitle: string;
  warningText?: string;
  templateUrl: string;
  templateFileName: string;
  onUpload: (arquivo: File) => Promise<ImportacaoResult>;
  onFinished: () => void;
  onClose: () => void;
  danger?: boolean;
}

export function BulkOperationModal({
  open,
  title,
  subtitle,
  warningText,
  templateUrl,
  templateFileName,
  onUpload,
  onFinished,
  onClose,
  danger,
}: BulkOperationModalProps) {
  const [arquivo, setArquivo] = useState<File | null>(null);
  const [processando, setProcessando] = useState(false);
  const [resultado, setResultado] = useState<ImportacaoResult | null>(null);

  function handleClose() {
    setArquivo(null);
    setResultado(null);
    onClose();
  }

  async function handleProcessar() {
    if (!arquivo) {
      message.warning('Selecione um arquivo .xlsx antes de processar.');
      return;
    }

    try {
      setProcessando(true);
      const resultado = await onUpload(arquivo);
      setResultado(resultado);
      if (resultado.erros.length === 0) {
        message.success('Arquivo processado com sucesso.');
      }
      onFinished();
    } catch (error) {
      message.error(extrairMensagemErro(error, 'Não foi possível processar o arquivo.'));
    } finally {
      setProcessando(false);
    }
  }

  const uploadFileList: UploadFile[] = arquivo
    ? [{ uid: '-1', name: arquivo.name, status: 'done' } as UploadFile]
    : [];

  return (
    <Modal
      title={title}
      open={open}
      onCancel={handleClose}
      footer={[
        <Button key="fechar" onClick={handleClose}>
          Fechar
        </Button>,
        <Button key="processar" type="primary" danger={danger} loading={processando} onClick={handleProcessar}>
          Processar arquivo
        </Button>,
      ]}
      destroyOnHidden
    >
      <Paragraph type="secondary" style={{ marginTop: -8 }}>
        {subtitle}
      </Paragraph>

      {warningText && <Alert type="warning" showIcon message={warningText} style={{ marginBottom: 16 }} />}

      <div style={{ background: '#fafafa', border: '1px solid #f0f0f0', borderRadius: 8, padding: 16, marginBottom: 16 }}>
        <Text strong>1. Baixe o arquivo modelo</Text>
        <Paragraph type="secondary" style={{ marginTop: 4 }}>
          Preencha a planilha com os dados antes de enviar.
        </Paragraph>
        <a href={templateUrl} download={templateFileName}>
          <Button icon={<DownloadOutlined />}>Baixar modelo Excel (.xlsx)</Button>
        </a>
      </div>

      <div style={{ background: '#fafafa', border: '1px solid #f0f0f0', borderRadius: 8, padding: 16 }}>
        <Text strong>2. Envie o arquivo preenchido</Text>
        <Paragraph type="secondary" style={{ marginTop: 4 }}>
          Apenas arquivos .xlsx são aceitos.
        </Paragraph>
        <Upload.Dragger
          accept=".xlsx"
          maxCount={1}
          fileList={uploadFileList}
          beforeUpload={(file) => {
            setArquivo(file);
            setResultado(null);
            return false;
          }}
          onRemove={() => setArquivo(null)}
        >
          <p className="ant-upload-drag-icon">
            <InboxOutlined />
          </p>
          <p>Selecionar arquivo .xlsx</p>
        </Upload.Dragger>
      </div>

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
              style={{ marginTop: 8, maxHeight: 200, overflowY: 'auto' }}
              bordered
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
    </Modal>
  );
}

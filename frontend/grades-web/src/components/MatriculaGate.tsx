import { Form, Input, Modal, Typography } from 'antd';
import { useEffect, useState } from 'react';
import { definirMatricula, registrarSolicitador, validarFormatoMatricula } from '../auth/matricula';

const { Text } = Typography;

interface FormValues {
  matricula: string;
}

// Fica montado uma vez perto da raiz do app (ver App.tsx). Não faz nada
// sozinho: o interceptor do axios em gradesApi.ts é quem decide quando a
// matrícula é necessária e aciona `registrarSolicitador` para abrir este modal.
export function MatriculaGate() {
  const [form] = Form.useForm<FormValues>();
  const [aberto, setAberto] = useState(false);

  useEffect(() => {
    registrarSolicitador(() => setAberto(true));
  }, []);

  function handleConfirmar() {
    form.validateFields().then((valores) => {
      definirMatricula(valores.matricula.trim());
      setAberto(false);
    });
  }

  return (
    <Modal
      title="Identificação necessária"
      open={aberto}
      onOk={handleConfirmar}
      okText="Confirmar"
      closable={false}
      maskClosable={false}
      cancelButtonProps={{ style: { display: 'none' } }}
      destroyOnHidden
    >
      <Text type="secondary">
        Informe sua matrícula para registrar esta alteração no histórico de auditoria.
      </Text>

      <Form form={form} layout="vertical" style={{ marginTop: 16 }} onFinish={handleConfirmar}>
        <Form.Item
          label="Matrícula"
          name="matricula"
          rules={[{ validator: (_, valor) => {
            const erro = validarFormatoMatricula(valor ?? '');
            return erro ? Promise.reject(new Error(erro)) : Promise.resolve();
          } }]}
        >
          <Input placeholder="Ex: 12345" autoFocus maxLength={20} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

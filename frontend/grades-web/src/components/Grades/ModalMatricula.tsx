import React, { useEffect, useState } from 'react';
import { Form, Input, Modal, Typography } from 'antd';
import { definirMatricula, registrarSolicitador, validarFormatoMatricula } from '../../utils/matricula';

interface FormValues {
  matricula: string;
}

// Montado uma vez perto da raiz (App.tsx). Não faz nada sozinho: o
// interceptor de services/gradeApi.ts é quem decide quando a matrícula é
// necessária (e ainda não existe) e aciona `registrarSolicitador` para abrir este modal.
const ModalMatricula: React.FC = () => {
  const [form] = Form.useForm<FormValues>();
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    registrarSolicitador(() => setIsOpen(true));
  }, []);

  const handleConfirm = async () => {
    try {
      const valores = await form.validateFields();
      definirMatricula(valores.matricula);
      setIsOpen(false);
    } catch {
      // erro de validação já exibido no campo
    }
  };

  return (
    <Modal
      title="Identificação Necessária"
      open={isOpen}
      onOk={handleConfirm}
      okText="Confirmar"
      closable={false}
      maskClosable={false}
      keyboard={false}
      cancelButtonProps={{ style: { display: 'none' } }}
      destroyOnClose
    >
      <Typography.Text type="secondary">Informe sua matrícula para registrar esta alteração no histórico de auditoria.</Typography.Text>

      <Form form={form} layout="vertical" style={{ marginTop: 16 }} onFinish={handleConfirm} preserve={false}>
        <Form.Item
          label="Matrícula"
          name="matricula"
          rules={[
            {
              validator: (_, valor) => {
                const erro = validarFormatoMatricula(valor ?? '');
                return erro ? Promise.reject(new Error(erro)) : Promise.resolve();
              },
            },
          ]}
        >
          <Input placeholder="Ex: 12345" autoFocus maxLength={20} />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default ModalMatricula;

import { Form, Input, Modal, message } from 'antd';
import { useState } from 'react';
import { atualizarGrade, criarGrade, extrairMensagemErro } from '../api/gradesApi';
import type { Grade, GradeFormValues } from '../types/grade';

interface GradeFormModalProps {
  open: boolean;
  grade?: Grade | null;
  onClose: () => void;
  onSaved: () => void;
}

export function GradeFormModal({ open, grade, onClose, onSaved }: GradeFormModalProps) {
  const [form] = Form.useForm<GradeFormValues>();
  const [salvando, setSalvando] = useState(false);
  const editando = Boolean(grade);

  async function handleSalvar() {
    try {
      const valores = await form.validateFields();
      setSalvando(true);

      if (editando && grade) {
        await atualizarGrade(grade.codigo, valores);
        message.success('Grade atualizada com sucesso.');
      } else {
        await criarGrade(valores);
        message.success('Grade criada com sucesso.');
      }

      onSaved();
    } catch (error) {
      if (error instanceof Error === false) return; // erro de validação do antd, já exibido nos campos
      message.error(extrairMensagemErro(error, 'Não foi possível salvar a grade.'));
    } finally {
      setSalvando(false);
    }
  }

  return (
    <Modal
      title={editando ? 'Editar grade' : 'Nova grade'}
      open={open}
      onCancel={onClose}
      onOk={handleSalvar}
      confirmLoading={salvando}
      okText="Salvar grade"
      cancelText="Cancelar"
      destroyOnHidden
    >
      <p style={{ color: 'rgba(0,0,0,0.45)', marginTop: -8 }}>Preencha os campos obrigatórios</p>

      <Form
        form={form}
        layout="vertical"
        preserve={false}
        initialValues={{ nome: grade?.nome ?? '', sigla: grade?.sigla ?? '' }}
      >
        <Form.Item label="Código sequencial">
          <Input value={grade?.codigo ? String(grade.codigo) : ''} placeholder="— gerado automaticamente —" disabled readOnly />
        </Form.Item>

        <Form.Item
          label="Nome"
          name="nome"
          rules={[{ required: true, message: 'Informe o nome da grade.' }, { max: 80, message: 'Máximo de 80 caracteres.' }]}
        >
          <Input placeholder="Ex: CONDICIONADOR SEDA" showCount maxLength={80} />
        </Form.Item>

        <Form.Item
          label="Sigla"
          name="sigla"
          rules={[{ required: true, message: 'Informe a sigla da grade.' }, { max: 15, message: 'Máximo de 15 caracteres.' }]}
        >
          <Input placeholder="Ex: COND SEDA" showCount maxLength={15} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

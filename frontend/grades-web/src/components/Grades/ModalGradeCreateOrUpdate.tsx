import React from 'react';
import { Form, Input, Modal } from 'antd';
import { useSelector } from 'react-redux';
import { fetchCreateGrade, fetchUpdateGrade } from '../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../store';
import type { Grade, GradeFormValues } from '../../store/grades/types';
import { alertError, alertSuccess } from '../../utils/alerts';
import { getErrorMessage } from '../../utils/apiError';

type Props = {
  isOpen: boolean;
  grade: Grade | null;
  handleCancel: () => void;
  onSaved: () => void;
};

const ModalGradeCreateOrUpdate: React.FC<Props> = ({ isOpen, grade, handleCancel, onSaved }) => {
  const dispatch = useAppDispatch();
  const [form] = Form.useForm<GradeFormValues>();
  const saving = useSelector((state: RootState) => state.grades.saving);

  const handleSave = async () => {
    let valores: GradeFormValues;
    try {
      valores = await form.validateFields();
    } catch {
      return; // erros de validação já aparecem nos campos
    }

    try {
      if (grade) {
        await dispatch(fetchUpdateGrade({ codigoGrade: grade.codigoGrade, valores })).unwrap();
        alertSuccess('Grade atualizada com sucesso.');
      } else {
        await dispatch(fetchCreateGrade(valores)).unwrap();
        alertSuccess('Grade criada com sucesso.');
      }
      onSaved();
    } catch (error) {
      alertError(getErrorMessage(error, 'Não foi possível salvar a grade.'));
    }
  };

  return (
    <Modal
      title={grade ? 'Editar Grade' : 'Nova Grade'}
      open={isOpen}
      onCancel={handleCancel}
      onOk={handleSave}
      confirmLoading={saving}
      okText="Salvar"
      cancelText="Cancelar"
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        preserve={false}
        initialValues={{ nome: grade?.nome ?? '', sigla: grade?.sigla ?? '' }}
      >
        <Form.Item label="Código sequencial">
          <Input value={grade ? String(grade.codigoGrade) : ''} placeholder="— gerado automaticamente —" disabled readOnly />
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
          rules={[{ required: true, message: 'Informe a sigla da grade.' }, { max: 35, message: 'Máximo de 35 caracteres.' }]}
        >
          <Input placeholder="Ex: COND SEDA" showCount maxLength={35} />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default ModalGradeCreateOrUpdate;

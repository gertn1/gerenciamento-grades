import React from 'react';
import { Modal } from 'antd';
import { DeleteOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import { fetchDeleteGrade } from '../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../store';
import type { GradeParaExclusao } from '../../store/grades/types';
import { alertError, alertSuccess } from '../../utils/alerts';
import { getErrorMessage } from '../../utils/apiError';

type Props = {
  isOpen: boolean;
  grade: GradeParaExclusao | null;
  handleCancel: () => void;
  onDeleted: () => void;
};

const ModalDeleteGrade: React.FC<Props> = ({ isOpen, grade, handleCancel, onDeleted }) => {
  const dispatch = useAppDispatch();
  const saving = useSelector((state: RootState) => state.grades.saving);

  const handleDelete = async () => {
    if (!grade) return;

    try {
      await dispatch(fetchDeleteGrade(grade.codigoGrade)).unwrap();
      alertSuccess('Grade excluída com sucesso.');
      onDeleted();
    } catch (error) {
      alertError(getErrorMessage(error, 'Não foi possível excluir a grade.'));
    }
  };

  return (
    <Modal
      open={isOpen}
      onCancel={handleCancel}
      onOk={handleDelete}
      confirmLoading={saving}
      okText="Excluir Grade"
      okButtonProps={{ danger: true }}
      cancelText="Cancelar"
      centered
      destroyOnClose
    >
      <div style={{ textAlign: 'center', padding: '8px 0' }}>
        <DeleteOutlined style={{ fontSize: 48, color: 'rgba(0,0,0,0.25)' }} />
        <h3 style={{ marginTop: 16 }}>Excluir grade?</h3>
        {grade && (
          <>
            <p>
              A grade <strong>#{grade.codigoGrade} — {grade.nome}</strong> será excluída permanentemente.
            </p>
            <p>
              Os <strong>{grade.qtdSkus} SKU(s)</strong> vinculados perderão o vínculo e ficarão sem grade.
              <br />
              <strong>Esta ação é irreversível.</strong>
            </p>
          </>
        )}
      </div>
    </Modal>
  );
};

export default ModalDeleteGrade;

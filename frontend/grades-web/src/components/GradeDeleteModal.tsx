import { DeleteOutlined } from '@ant-design/icons';
import { Modal, message } from 'antd';
import { useState } from 'react';
import { excluirGrade, extrairMensagemErro } from '../api/gradesApi';

interface GradeParaExclusao {
  codigo: number;
  nome: string;
  qtdSkus: number;
}

interface GradeDeleteModalProps {
  open: boolean;
  grade: GradeParaExclusao | null;
  onClose: () => void;
  onDeleted: () => void;
}

export function GradeDeleteModal({ open, grade, onClose, onDeleted }: GradeDeleteModalProps) {
  const [excluindo, setExcluindo] = useState(false);

  async function handleExcluir() {
    if (!grade) return;

    try {
      setExcluindo(true);
      await excluirGrade(grade.codigo);
      message.success('Grade excluída com sucesso.');
      onDeleted();
    } catch (error) {
      message.error(extrairMensagemErro(error, 'Não foi possível excluir a grade.'));
    } finally {
      setExcluindo(false);
    }
  }

  return (
    <Modal
      open={open}
      onCancel={onClose}
      onOk={handleExcluir}
      confirmLoading={excluindo}
      okText="Excluir grade"
      okButtonProps={{ danger: true }}
      cancelText="Cancelar"
      centered
      destroyOnHidden
    >
      <div style={{ textAlign: 'center', padding: '8px 0' }}>
        <DeleteOutlined style={{ fontSize: 48, color: 'rgba(0,0,0,0.25)' }} />
        <h3 style={{ marginTop: 16 }}>Excluir grade?</h3>
        {grade && (
          <>
            <p>
              A grade <strong>#{grade.codigo} — {grade.nome}</strong> será excluída permanentemente.
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
}

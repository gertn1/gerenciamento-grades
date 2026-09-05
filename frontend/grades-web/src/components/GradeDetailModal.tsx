import { Empty, Modal, Skeleton, Tag } from 'antd';
import { useEffect, useState } from 'react';
import { obterDetalheGrade } from '../api/gradesApi';
import type { GradeDetalhe } from '../types/grade';

interface GradeDetailModalProps {
  open: boolean;
  codigo: number | null;
  onClose: () => void;
}

export function GradeDetailModal({ open, codigo, onClose }: GradeDetailModalProps) {
  const [carregando, setCarregando] = useState(false);
  const [detalhe, setDetalhe] = useState<GradeDetalhe | null>(null);

  useEffect(() => {
    if (!open || !codigo) return;

    setCarregando(true);
    setDetalhe(null);
    obterDetalheGrade(codigo)
      .then(setDetalhe)
      .finally(() => setCarregando(false));
  }, [open, codigo]);

  return (
    <Modal title="SKUs vinculados" open={open} onCancel={onClose} footer={null} destroyOnHidden>
      {carregando && <Skeleton active />}

      {!carregando && detalhe && (
        <>
          <p style={{ color: 'rgba(0,0,0,0.45)' }}>
            #{detalhe.codigo} — {detalhe.nome} ({detalhe.sigla})
          </p>

          {detalhe.skus.length === 0 ? (
            <Empty description="Nenhum SKU vinculado a esta grade." />
          ) : (
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, maxHeight: 320, overflowY: 'auto' }}>
              {detalhe.skus.map((sku) => (
                <Tag key={sku}>{sku}</Tag>
              ))}
            </div>
          )}
        </>
      )}
    </Modal>
  );
}

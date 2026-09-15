import React from 'react';
import { Modal, Tabs } from 'antd';
import type { TabsProps } from 'antd';
import { useSelector } from 'react-redux';
import TabPaneSkusOrfaos from './TabPaneSkusOrfaos';
import TabPaneGradesVazias from './TabPaneGradesVazias';
import { type RootState, useAppDispatch } from '../../../store';
import { clearDiagnostico } from '../../../store/grades/diagnosticoSlice';

type Props = {
  isOpen: boolean;
  handleCancel: () => void;
  onAbrirGrade: (codigoGrade: number) => void;
};

const ModalDiagnostico: React.FC<Props> = ({ isOpen, handleCancel, onAbrirGrade }) => {
  const dispatch = useAppDispatch();
  const totalSkusOrfaos = useSelector((state: RootState) => state.diagnostico.skusOrfaos.total);
  const totalGradesVazias = useSelector((state: RootState) => state.diagnostico.gradesVazias.length);

  // forceRender: as duas abas carregam ao abrir o modal, para os contadores
  // dos títulos já aparecerem sem precisar clicar em cada aba.
  const items: TabsProps['items'] = [
    {
      key: 'orfaos',
      label: `SKUs órfãos (${totalSkusOrfaos})`,
      children: <TabPaneSkusOrfaos />,
      forceRender: true,
    },
    {
      key: 'vazias',
      label: `Grades vazias (${totalGradesVazias})`,
      children: <TabPaneGradesVazias onAbrirGrade={onAbrirGrade} />,
      forceRender: true,
    },
  ];

  return (
    <Modal
      title="SKUs Órfãos e Grades Vazias"
      open={isOpen}
      onCancel={handleCancel}
      afterClose={() => dispatch(clearDiagnostico())}
      footer={false}
      width={760}
      destroyOnClose
    >
      <Tabs defaultActiveKey="orfaos" items={items} />
    </Modal>
  );
};

export default ModalDiagnostico;

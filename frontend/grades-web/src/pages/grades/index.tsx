import React, { useEffect, useState } from 'react';
import { Button, Col, Divider, Row, Space } from 'antd';
import { FileExcelOutlined, FileSearchOutlined, ImportOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import PageHeader from '../../components/layout/PageHeader';
import FormGrade from '../../components/Grades/FormGrade';
import DataTableGrades from '../../components/Grades/DataTableGrades';
import ModalGradeCreateOrUpdate from '../../components/Grades/ModalGradeCreateOrUpdate';
import ModalGradeDetails from '../../components/Grades/ModalGradeDetails';
import ModalDeleteGrade from '../../components/Grades/ModalDeleteGrade';
import ModalImportacaoMassiva from '../../components/Grades/ModalImportacaoMassiva';
import ModalDiagnostico from '../../components/Grades/Diagnostico/ModalDiagnostico';
import { fetchGrades } from '../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../store';
import type { Grade, GradeParaExclusao, TipoImportacaoMassiva } from '../../store/grades/types';
import { alertError } from '../../utils/alerts';
import { getErrorMessage } from '../../utils/apiError';

const PageGrades: React.FC = () => {
  const dispatch = useAppDispatch();
  const searchParams = useSelector((state: RootState) => state.grades.searchParams);

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [gradeEmEdicao, setGradeEmEdicao] = useState<Grade | null>(null);

  const [isDetailOpen, setIsDetailOpen] = useState(false);
  const [codigoGradeDetalhe, setCodigoGradeDetalhe] = useState<number | null>(null);

  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [gradeParaExcluir, setGradeParaExcluir] = useState<GradeParaExclusao | null>(null);

  const [isDiagnosticoOpen, setIsDiagnosticoOpen] = useState(false);
  const [importacao, setImportacao] = useState<{ isOpen: boolean; tipo: TipoImportacaoMassiva }>({ isOpen: false, tipo: 'criacao' });

  useEffect(() => {
    dispatch(fetchGrades({}))
      .unwrap()
      .catch((error) => alertError(getErrorMessage(error, 'Não foi possível carregar as grades.')));
  }, [dispatch]);

  // Recarrega mantendo o último filtro pesquisado (quantidade de SKUs muda após operações).
  const recarregarGrades = () => {
    dispatch(fetchGrades(searchParams))
      .unwrap()
      .catch((error) => alertError(getErrorMessage(error, 'Não foi possível carregar as grades.')));
  };

  const abrirNovaGrade = () => {
    setGradeEmEdicao(null);
    setIsFormOpen(true);
  };

  const abrirEdicao = (grade: Grade) => {
    setGradeEmEdicao({ codigoGrade: grade.codigoGrade, nome: grade.nome, sigla: grade.sigla });
    setIsFormOpen(true);
  };

  const abrirDetalhes = (codigoGrade: number) => {
    setCodigoGradeDetalhe(codigoGrade);
    setIsDetailOpen(true);
  };

  const abrirExclusao = (grade: GradeParaExclusao) => {
    setGradeParaExcluir({ codigoGrade: grade.codigoGrade, nome: grade.nome, qtdSkus: grade.qtdSkus });
    setIsDeleteOpen(true);
  };

  const abrirImportacao = (tipo: TipoImportacaoMassiva) => setImportacao({ isOpen: true, tipo });

  return (
    <>
      <Row justify="space-between" align="middle" style={{ marginBottom: 20 }}>
        <Col>
          <PageHeader title="Gerenciamento de Grades" buttonText="" />
        </Col>
        <Col>
          <Space size={8} wrap>
            <Button type="primary" ghost onClick={() => setIsDiagnosticoOpen(true)} icon={<FileSearchOutlined />}>
              Diagnóstico
            </Button>
            <Button type="primary" ghost onClick={() => abrirImportacao('criacao')} icon={<FileExcelOutlined />}>
              Criação Massiva de Grades
            </Button>
            <Button type="primary" ghost onClick={() => abrirImportacao('atualizacao')} icon={<ImportOutlined />}>
              Importação Massiva
            </Button>
            <Button type="primary" ghost danger onClick={() => abrirImportacao('exclusao')} icon={<UploadOutlined />}>
              Exclusão Massiva de SKUs
            </Button>
            <Button type="primary" onClick={abrirNovaGrade} icon={<PlusOutlined />}>
              Nova Grade
            </Button>
          </Space>
        </Col>
      </Row>
      <Divider />

      <FormGrade />
      <DataTableGrades
        onDetalhes={(grade) => abrirDetalhes(grade.codigoGrade)}
        onEditar={abrirEdicao}
        onExcluir={abrirExclusao}
      />

      <ModalGradeCreateOrUpdate
        isOpen={isFormOpen}
        grade={gradeEmEdicao}
        handleCancel={() => setIsFormOpen(false)}
        onSaved={() => {
          setIsFormOpen(false);
          recarregarGrades();
        }}
      />

      <ModalGradeDetails
        isOpen={isDetailOpen}
        codigoGrade={codigoGradeDetalhe}
        handleCancel={() => setIsDetailOpen(false)}
        onEditarGrade={abrirEdicao}
        onExcluirGrade={(grade) => abrirExclusao({ codigoGrade: grade.codigoGrade, nome: grade.nome, qtdSkus: grade.skus.length })}
        onSkusAlterados={recarregarGrades}
      />

      <ModalDeleteGrade
        isOpen={isDeleteOpen}
        grade={gradeParaExcluir}
        handleCancel={() => setIsDeleteOpen(false)}
        onDeleted={() => {
          setIsDeleteOpen(false);
          setIsDetailOpen(false);
        }}
      />

      <ModalImportacaoMassiva
        isOpen={importacao.isOpen}
        tipo={importacao.tipo}
        handleCancel={() => setImportacao((atual) => ({ ...atual, isOpen: false }))}
        onFinished={recarregarGrades}
      />

      <ModalDiagnostico
        isOpen={isDiagnosticoOpen}
        handleCancel={() => setIsDiagnosticoOpen(false)}
        onAbrirGrade={(codigoGrade) => {
          setIsDiagnosticoOpen(false);
          abrirDetalhes(codigoGrade);
        }}
      />
    </>
  );
};

export default PageGrades;

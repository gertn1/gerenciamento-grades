import React, { useEffect, useState } from 'react';
import { Button, Col, Input, InputNumber, Row, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { SearchOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import { fetchGradesVazias } from '../../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../../store';
import type { GradeListItem } from '../../../store/grades/types';
import { alertError } from '../../../utils/alerts';
import { getErrorMessage } from '../../../utils/apiError';

type Props = {
  onAbrirGrade: (codigoGrade: number) => void;
};

const TabPaneGradesVazias: React.FC<Props> = ({ onAbrirGrade }) => {
  const dispatch = useAppDispatch();
  const gradesVazias = useSelector((state: RootState) => state.diagnostico.gradesVazias);
  const loading = useSelector((state: RootState) => state.diagnostico.loadingGradesVazias);

  const [codigoGrade, setCodigoGrade] = useState<number | null>(null);
  const [nome, setNome] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => {
      dispatch(fetchGradesVazias({ codigoGrade: codigoGrade ?? undefined, nome: nome.trim() || undefined }))
        .unwrap()
        .catch((error) => alertError(getErrorMessage(error, 'Não foi possível carregar as grades vazias.')));
    }, 350);

    return () => clearTimeout(timer);
  }, [codigoGrade, nome, dispatch]);

  const columns: ColumnsType<GradeListItem> = [
    { title: 'Código', dataIndex: 'codigoGrade', width: 90, align: 'center' },
    { title: 'Nome', dataIndex: 'nome' },
    { title: 'Sigla', dataIndex: 'sigla', width: 140 },
    {
      title: '',
      width: 80,
      align: 'center',
      render: (_, grade) => (
        <Button type="link" size="small" onClick={() => onAbrirGrade(grade.codigoGrade)}>
          Abrir
        </Button>
      ),
    },
  ];

  return (
    <>
      <Typography.Text type="secondary">Grades cadastradas sem nenhum SKU vinculado.</Typography.Text>

      <Row gutter={12} style={{ marginTop: 12 }}>
        <Col>
          <InputNumber placeholder="Código" value={codigoGrade} onChange={setCodigoGrade} min={1} style={{ width: 120 }} />
        </Col>
        <Col flex="1 1 auto">
          <Input placeholder="Buscar por nome da grade..." prefix={<SearchOutlined />} value={nome} onChange={(e) => setNome(e.target.value)} allowClear />
        </Col>
      </Row>

      <Table
        style={{ marginTop: 12 }}
        size="small"
        bordered
        columns={columns}
        dataSource={gradesVazias}
        rowKey="codigoGrade"
        loading={loading}
        pagination={{ pageSize: 10, hideOnSinglePage: true }}
      />
    </>
  );
};

export default TabPaneGradesVazias;

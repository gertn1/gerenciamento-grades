import React from 'react';
import { Button, Table, Tooltip, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DeleteOutlined, EditOutlined, FileSearchOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import type { RootState } from '../../store';
import type { GradeListItem } from '../../store/grades/types';

interface DataTableGradesProps {
  onDetalhes: (grade: GradeListItem) => void;
  onEditar: (grade: GradeListItem) => void;
  onExcluir: (grade: GradeListItem) => void;
}

const DataTableGrades: React.FC<DataTableGradesProps> = ({ onDetalhes, onEditar, onExcluir }) => {
  const grades = useSelector((state: RootState) => state.grades.data);
  const loading = useSelector((state: RootState) => state.grades.loading);

  const columns: ColumnsType<GradeListItem> = [
    {
      title: 'Código',
      dataIndex: 'codigoGrade',
      align: 'center',
      width: 110,
    },
    {
      title: 'Nome',
      dataIndex: 'nome',
    },
    {
      title: 'Sigla',
      dataIndex: 'sigla',
      render: (sigla: string) => <Typography.Text code>{sigla}</Typography.Text>,
    },
    {
      title: 'Quantidade de SKUs',
      dataIndex: 'qtdSkus',
      align: 'center',
      width: 170,
    },
    {
      title: '',
      render: (_, grade) => (
        <>
          <Tooltip title="Detalhes">
            <Button shape="circle" icon={<FileSearchOutlined />} onClick={() => onDetalhes(grade)} />
          </Tooltip>{' '}
          <Tooltip title="Editar">
            <Button shape="circle" icon={<EditOutlined />} onClick={() => onEditar(grade)} />
          </Tooltip>{' '}
          <Tooltip title="Excluir">
            <Button shape="circle" danger icon={<DeleteOutlined />} onClick={() => onExcluir(grade)} />
          </Tooltip>
        </>
      ),
      align: 'center',
      width: '180px',
    },
  ];

  return (
    <Table
      rowKey="codigoGrade"
      size="small"
      bordered
      loading={loading}
      columns={columns}
      dataSource={grades}
      pagination={{ pageSize: 10, showSizeChanger: false, showTotal: (total) => `${total} grade(s) encontrada(s)` }}
    />
  );
};

export default DataTableGrades;

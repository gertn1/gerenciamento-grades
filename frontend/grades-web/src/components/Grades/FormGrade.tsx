import React from 'react';
import { Button, Col, Form, Input, InputNumber, Row } from 'antd';
import { SearchOutlined, SyncOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import { fetchGrades } from '../../services/gradeApi';
import { type RootState, useAppDispatch } from '../../store';
import type { GradeSearchParams } from '../../store/grades/types';
import { alertError } from '../../utils/alerts';
import { getErrorMessage } from '../../utils/apiError';

interface FormValues {
  codigoGrade: number | null;
  nome: string;
}

const initialValues: FormValues = {
  codigoGrade: null,
  nome: '',
};

const FormGrade: React.FC = () => {
  const dispatch = useAppDispatch();
  const [form] = Form.useForm<FormValues>();
  const loading = useSelector((state: RootState) => state.grades.loading);

  const pesquisar = (params: GradeSearchParams) => {
    dispatch(fetchGrades(params))
      .unwrap()
      .catch((error) => alertError(getErrorMessage(error, 'Não foi possível carregar as grades.')));
  };

  const handleFinish = (values: FormValues) => {
    pesquisar({
      codigoGrade: values.codigoGrade ?? undefined,
      nome: values.nome?.trim() || undefined,
    });
  };

  const handleClear = () => {
    form.resetFields();
    pesquisar({});
  };

  return (
    <Form onFinish={handleFinish} form={form} layout="vertical" initialValues={initialValues}>
      <Row gutter={32}>
        <Col flex="0 0 220px">
          <Form.Item label="Código da Grade" name="codigoGrade">
            <InputNumber placeholder="Código" min={1} style={{ width: '100%' }} />
          </Form.Item>
        </Col>
        <Col flex={1}>
          <Form.Item label="Nome" name="nome">
            <Input placeholder="Nome da grade" allowClear />
          </Form.Item>
        </Col>
      </Row>

      <Row justify="end" gutter={6}>
        <Col>
          <Button type="primary" ghost onClick={handleClear} icon={<SyncOutlined />}>
            Limpar
          </Button>
        </Col>
        <Col>
          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loading} icon={<SearchOutlined />}>
              Pesquisar
            </Button>
          </Form.Item>
        </Col>
      </Row>
    </Form>
  );
};

export default FormGrade;

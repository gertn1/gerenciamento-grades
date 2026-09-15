import React from 'react';
import { Button, Col, Row, Tooltip } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';

interface Props {
  title: string;
  buttonText: string;
  action?: () => void;
  displayButton?: boolean;
  buttonBack?: boolean;
  disabled?: boolean;
  tooltipText?: string;
  iconButton?: React.ReactNode;
}

const PageHeader: React.FC<Props> = ({ title, buttonText, action, displayButton = true, buttonBack, disabled = false, tooltipText, iconButton }) => {
  const history = useNavigate();
  return (
    <div>
      {buttonBack ? (
        <Row justify="space-between" align="middle" style={{ marginBottom: 20 }}>
          <Button type="primary" ghost shape="round" icon={<ArrowLeftOutlined />} onClick={() => history(-1)}>
            Voltar
          </Button>
        </Row>
      ) : (
        ''
      )}
      <Row justify="space-between" align="middle" style={{ padding: '5px 0px' }}>
        <Col span={8} style={{ display: 'flex', justifyContent: 'left' }}>
          <h3 style={{ fontWeight: 'bold', color: 'rgb(1, 87, 155)', fontSize: 35, whiteSpace: 'nowrap' }}>{title}</h3>
        </Col>
        {buttonText === '' ? (
          ''
        ) : (
          <Col span={4} offset={12} style={{ display: displayButton ? 'flex' : 'none', justifyContent: 'right' }}>
            <Tooltip title={tooltipText} placement="top">
              <Button disabled={disabled} type="primary" icon={iconButton} onClick={action}>
                {buttonText}
              </Button>
            </Tooltip>
          </Col>
        )}
      </Row>
    </div>
  );
};

export default PageHeader;

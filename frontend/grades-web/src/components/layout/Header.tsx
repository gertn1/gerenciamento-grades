import React, { useEffect, useState } from 'react';
import { Button, Col, Layout } from 'antd';
import { useNavigate } from 'react-router-dom';
import img from '../../assets/logo-pmenos-branca.png';
import {
  esquecerMatricula,
  garantirMatricula,
  matriculaVemDoPrecoWeb,
  obterMatriculaAtual,
  obterNomeUsuarioPrecoWeb,
  ouvirMatricula,
} from '../../utils/matricula';

const TITLE = process.env.REACT_APP_NAME || '';

const { Header } = Layout;

const AppHeader: React.FC = () => {
  const [matricula, setMatricula] = useState(obterMatriculaAtual());
  const history = useNavigate();
  const doPrecoWeb = matriculaVemDoPrecoWeb();
  const userName = obterNomeUsuarioPrecoWeb();

  useEffect(() => ouvirMatricula(setMatricula), []);

  const handleTrocarMatricula = () => {
    esquecerMatricula();
    garantirMatricula();
  };

  return (
    <Header id="header" title={TITLE} style={{ backgroundColor: '#005f99', display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 15px', height: '50px' }}>
      <div style={{ flex: 1, textAlign: 'left' }}>
        <img src={img} height={20} onClick={() => history('/')} alt="logo" style={{ cursor: 'pointer' }} />
      </div>
      <h2 style={{ flex: 1, textAlign: 'center', margin: '0', color: 'white', fontFamily: 'Arial, sans-serif', fontWeight: 'bold', fontSize: '16px' }}>Preços WEB</h2>
      <Col style={{ flex: 1, textAlign: 'right', color: 'white', fontFamily: 'Arial, sans-serif', fontWeight: 'normal', fontSize: '12px' }}>
        {matricula ? <span>{userName ? `${matricula} - ${userName}` : `Matrícula ${matricula}`}</span> : <span>Identificação pendente</span>}
        {!doPrecoWeb && (
          <Button type="link" size="small" onClick={handleTrocarMatricula} style={{ color: 'white', textDecoration: 'underline', fontSize: '12px' }}>
            trocar
          </Button>
        )}
      </Col>
    </Header>
  );
};

export default AppHeader;

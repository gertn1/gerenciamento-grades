import React from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import Root from '../pages/shared/Root';
import PageGrades from '../pages/grades';
import PrivateRoute from './PrivateRouter';

const AppRoutes: React.FC = () => (
  <Root>
    <Routes>
      <Route path="/" element={<Navigate to="/grades" replace />} />
      <Route path="/grades" element={<PrivateRoute><PageGrades /></PrivateRoute>} />
      <Route path="*" element={<div>Página não encontrada</div>} />
    </Routes>
  </Root>
);

export default AppRoutes;

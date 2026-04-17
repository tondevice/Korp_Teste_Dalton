import { Routes } from '@angular/router';
import { Layout } from './shared/layout/layout';
import { Produtos } from './pages/produtos/produtos';
import { Notas } from './pages/notas/notas';

export const routes: Routes = [
  {
    path: '',
    component: Layout,
    children: [
      { path: '', redirectTo: 'produtos', pathMatch: 'full' },
      { path: 'produtos', component: Produtos },
      { path: 'notas', component: Notas }
    ]
  }
];
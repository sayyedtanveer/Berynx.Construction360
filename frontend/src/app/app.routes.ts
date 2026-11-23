import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.page').then((module) => module.HomePage),
    title: 'Berynx Construction 360',
  },
  { path: '**', redirectTo: '' },
];

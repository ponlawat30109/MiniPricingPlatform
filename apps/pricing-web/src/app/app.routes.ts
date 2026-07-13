import { Routes } from '@angular/router';
import { OperationsShellComponent } from './layouts/operations-shell.component';
export const routes: Routes = [
  {
    path: '',
    component: OperationsShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'quotes' },
      {
        path: 'quotes',
        loadComponent: () =>
          import('./features/quotes/quotes.component').then((m) => m.QuotesComponent),
      },
      {
        path: 'rules',
        loadComponent: () =>
          import('./features/rules/rules.component').then((m) => m.RulesComponent),
      },
      {
        path: 'jobs',
        loadComponent: () => import('./features/jobs/jobs.component').then((m) => m.JobsComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'quotes' },
];
